// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;
using Newtonsoft.Json.Linq;

namespace Microsoft.Framework.DesignTimeHost
{
    public interface IPlugin
    {
        void ProcessMessage(JObject data, IAssemblyLoadContext assemblyLoadContext);
    }
}