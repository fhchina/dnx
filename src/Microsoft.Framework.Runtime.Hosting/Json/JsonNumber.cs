﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Framework.Runtime.Json
{
    internal class JsonNumber : JsonValue
    {
        private readonly string _raw;
        private readonly double _double;

        public JsonNumber(JsonToken token)
            : base(token.Line, token.Column)
        {
            try
            {
                _raw = token.Value;
                _double = double.Parse(_raw, NumberStyles.Float);
            }
            catch (FormatException ex)
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_InvalidFloatNumberFormat(_raw),
                    ex,
                    token.Line,
                    token.Column);
            }
            catch (OverflowException ex)
            {
                throw new JsonDeserializerException(
                    JsonDeserializerResource.Format_FloatNumberOverflow(_raw),
                    ex,
                    token.Line,
                    token.Column);
            }
        }

        public double Double
        {
            get { return _double; }
        }

        public int Int
        {
            get { return (int)_double; }
        }
    }
}
