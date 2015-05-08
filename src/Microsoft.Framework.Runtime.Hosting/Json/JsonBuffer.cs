﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Framework.Runtime.Json
{
    internal class JsonBuffer
    {
        private readonly TextReader _reader;
        private JsonToken _token;
        private int _line;
        private int _column;

        public JsonBuffer(TextReader reader)
        {
            _reader = reader;
            _line = 1;
        }

        public JsonToken Read()
        {
            int first;
            while (true)
            {
                first = ReadNextChar();

                if (first == -1)
                {
                    _token.Type = JsonTokenType.EOL;
                    return _token;
                }
                else if (!IsWhitespace(first))
                {
                    break;
                }
            }

            _token.Value = null;
            _token.Line = _line;
            _token.Column = _column;

            if (first == '{')
            {
                _token.Type = JsonTokenType.LeftCurlyBracket;
            }
            else if (first == '}')
            {
                _token.Type = JsonTokenType.RightCurlyBracket;
            }
            else if (first == '[')
            {
                _token.Type = JsonTokenType.LeftSquareBracket;
            }
            else if (first == ']')
            {
                _token.Type = JsonTokenType.RightSquareBracket;
            }
            else if (first == ':')
            {
                _token.Type = JsonTokenType.Colon;
            }
            else if (first == ',')
            {
                _token.Type = JsonTokenType.Comma;
            }
            else if (first == '"')
            {
                _token.Type = JsonTokenType.String;
                _token.Value = ReadString();
            }
            else if (first == 't')
            {
                ReadLiteral(JsonConstants.ValueTrue);
                _token.Type = JsonTokenType.True;
            }
            else if (first == 'f')
            {
                ReadLiteral(JsonConstants.ValueFalse);
                _token.Type = JsonTokenType.False;
            }
            else if (first == 'n')
            {
                ReadLiteral(JsonConstants.ValueNull);
                _token.Type = JsonTokenType.Null;
            }
            else if ((first >= '0' && first <= '9') || first == '-')
            {
                _token.Type = JsonTokenType.Number;
                _token.Value = ReadNumber(first);
            }
            else
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_IllegalCharacter(first),
                    _token);
            }

            // JsonToken is a value type
            return _token;
        }

        private int ReadNextChar()
        {
            while (true)
            {
                var value = _reader.Read();
                _column++;

                if (value == -1)
                {
                    // This is the end of file
                    return -1;
                }
                else if (value == JsonConstants.LineFeed)
                {
                    // This is a new line. Let the next loop read the first charactor of the following line.
                    // Set position ahead of next line
                    _column = 0;
                    _line++;

                    continue;
                }
                else if (value == JsonConstants.CarriageReturn)
                {
                    // Skip the carriage return.
                    // Let the next loop read the following char
                }
                else
                {
                    // Returns the normal value
                    return value;
                }
            }
        }

        private string ReadNumber(int firstRead)
        {
            var buf = new StringBuilder();
            buf.Append((char)firstRead);

            while (true)
            {
                var next = _reader.Peek();

                if ((next >= '0' && next <= '9') ||
                    next == '.' ||
                    next == 'e' ||
                    next == 'E')
                {
                    buf.Append((char)ReadNextChar());
                }
                else
                {
                    break;
                }
            }

            return buf.ToString();
        }

        private void ReadLiteral(string literal)
        {
            for (int i = 1; i < literal.Length; ++i)
            {
                var next = _reader.Peek();
                if (next != literal[i])
                {
                    throw new JsonDeserializerException(
                        JsonDeserializerResource.Format_UnrecognizedLiteral(literal),
                        _line, _column);
                }
                else
                {
                    ReadNextChar();
                }
            }

            var tail = _reader.Peek();
            if (tail != '}' &&
                tail != ']' &&
                tail != ',' &&
                tail != -1 &&
                tail != JsonConstants.CarriageReturn &&
                tail != JsonConstants.LineFeed &&
                !IsWhitespace(tail))
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_IllegalTrailingCharacterAfterLiteral(tail, literal),
                    _line, _column);
            }
        }

        private string ReadString()
        {
            var buf = new StringBuilder();
            var escaped = false;

            while (true)
            {
                var next = ReadNextChar();

                if (next == -1 || next == JsonConstants.LineFeed)
                {
                    throw new JsonDeserializerException(
                        JsonDeserializerResource.JSON_OpenString,
                        _line, _column);
                }
                else if (escaped)
                {
                    if ((next == '"') || (next == '\\') || (next == '/'))
                    {
                        buf.Append((char)next);
                    }
                    else if (next == 'b')
                    {
                        // '\b' backspace
                        buf.Append((char)JsonConstants.Backspace);
                    }
                    else if (next == 'f')
                    {
                        // '\f' form feed
                        buf.Append((char)JsonConstants.FormFeed);
                    }
                    else if (next == 'n')
                    {
                        // '\n' line feed
                        buf.Append((char)JsonConstants.LineFeed);
                    }
                    else if (next == 'r')
                    {
                        // '\r' carriage return
                        buf.Append((char)JsonConstants.CarriageReturn);
                    }
                    else if (next == 't')
                    {
                        // '\t' tab
                        buf.Append((char)JsonConstants.HorizontalTab);
                    }
                    else if (next == 'u')
                    {
                        // '\uXXXX' unicode
                        var unicodeLine = _line;
                        var unicodeColumn = _column;

                        var unicodesBuf = new StringBuilder(4);
                        for (int i = 0; i < 4; ++i)
                        {
                            next = ReadNextChar();
                            if (next == -1)
                            {
                                throw new JsonDeserializerException(
                                    JsonDeserializerResource.JSON_InvalidEnd,
                                    unicodeLine,
                                    unicodeColumn);
                            }
                            else
                            {
                                unicodesBuf[i] = (char)next;
                            }
                        }

                        try
                        {
                            var unicodeValue = int.Parse(unicodesBuf.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            buf.Append((char)unicodeValue);
                        }
                        catch (FormatException ex)
                        {
                            throw new JsonDeserializerException(
                                JsonDeserializerResource.Format_InvalidUnicode(unicodesBuf.ToString()),
                                ex,
                                unicodeLine,
                                unicodeColumn);
                        }
                    }
                    else
                    {
                        throw new JsonDeserializerException(
                            JsonDeserializerResource.Format_InvalidSyntaxNotExpected("charactor escape", "\\" + next),
                            _line,
                            _column);
                    }

                    escaped = false;
                }
                else if (next == '\\')
                {
                    escaped = true;
                }
                else if (next == '"')
                {
                    break;
                }
                else
                {
                    buf.Append((char)next);
                }
            }

            return buf.ToString();
        }

        private static bool IsWhitespace(int value)
        {
            return value == JsonConstants.Space || value == JsonConstants.HorizontalTab || value == JsonConstants.CarriageReturn;
        }
    }
}
