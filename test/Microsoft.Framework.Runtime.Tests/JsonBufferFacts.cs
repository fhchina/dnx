﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Framework.Runtime.Json.Tests
{
    public class JsonBufferFacts
    {
        [Fact]
        public void BasicReadToken()
        {
            var content = @"
            {
                ""key1"": ""value1"",
                ""key2"": 99,
                ""key3"": true,
                ""key4"": [""str1"", ""str2"", ""str3""],
                ""key5"": {
                    ""subkey1"": ""subvalue1"",
                    ""subkey2"": [1, 2]
                },
                ""key6"": null
            }";

            using (var stream = CreateTextReader(content))
            {
                var buffer = new JsonBuffer(stream);
                Assert.NotNull(buffer);

                while (buffer.Read().Type != JsonTokenType.EOL)
                    ;
            }
        }

        [Fact]
        public void ReadTokenThroughLockFileSample()
        {
            using (var stream = File.OpenRead(".\\TestSample\\project.lock.sample"))
            {
                var reader = new StreamReader(stream);
                var buffer = new JsonBuffer(reader);
                Assert.NotNull(buffer);

                while (buffer.Read().Type != JsonTokenType.EOL)
                    ;
            }
        }

        [Theory]
        [InlineData("true", JsonTokenType.True)]
        [InlineData(" true", JsonTokenType.True)]
        [InlineData("\ttrue", JsonTokenType.True)]
        [InlineData(" \ntrue", JsonTokenType.True)]
        [InlineData("false", JsonTokenType.False)]
        [InlineData("null", JsonTokenType.Null)]
        public void ReadLiteral(string content, JsonTokenType type)
        {
            using (var stream = CreateTextReader(content))
            {
                var buffer = new JsonBuffer(stream);
                Assert.NotNull(buffer);

                var token = buffer.Read();
                Assert.Equal(type, token.Type);
            }
        }

        [Theory]
        [InlineData("arue")]
        [InlineData("frue")]
        [InlineData(" arue")]
        [InlineData(" \nnrue")]
        [InlineData("false2")]
        public void ReadLiteralThrowException(string content)
        {
            using (var stream = CreateTextReader(content))
            {
                var buffer = new JsonBuffer(stream);
                Assert.NotNull(buffer);

                Assert.Throws<JsonDeserializerException>(() =>
                {
                    buffer.Read();
                });
            }
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[ ]")]
        [InlineData("[1]")]
        [InlineData("[\"1\"]")]
        [InlineData("[true]")]
        [InlineData("[true, false, null, 123, -123, 1.23e2, \"Hello, World\"]")]
        public void ReadThroughArray(string content)
        {
            using (var stream = CreateTextReader(content))
            {
                var buffer = new JsonBuffer(stream);
                Assert.NotNull(buffer);

                var tokens = new List<JsonToken>();
                do
                {
                    tokens.Add(buffer.Read());
                }
                while (tokens[tokens.Count - 1].Type != JsonTokenType.EOL);

                Assert.Equal(JsonTokenType.LeftSquareBracket, tokens[0].Type);
                Assert.Equal(JsonTokenType.RightSquareBracket, tokens[tokens.Count - 2].Type);
            }
        }

        private TextReader CreateTextReader(string content)
        {
            return new StringReader(content);
        }
    }
}
