// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.CommonTestUtils;
using Microsoft.Framework.Runtime;
using Xunit;

namespace Microsoft.Framework.PackageManager.FunctionalTests
{
    [Collection(nameof(PackageManagerFunctionalTestCollection))]
    public class DnuCommandsTests
    {
        private readonly PackageManagerFunctionalTestFixture _fixture;

        public DnuCommandsTests(PackageManagerFunctionalTestFixture fixture)
        {
            _fixture = fixture;
        }

        public static IEnumerable<object[]> RuntimeComponents
        {
            get
            {
                return TestUtils.GetRuntimeComponentsCombinations();
            }
        }

        [Theory]
        [MemberData(nameof(RuntimeComponents))]
        public void DnuCommands_Install_InstallsWorkingCommand(string flavor, string os, string architecture)
        {
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);

            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                var environment = new Dictionary<string, string>();
                environment.Add("USERPROFILE", $"{testEnv.RootDir}");

                string stdOut, stdErr;
                var exitCode = DnuTestUtils.ExecDnu(runtimeHomeDir, "commands", $"install {_fixture.PackageSource}/Debug/CommandsProject.1.0.0.nupkg",
                                                    out stdOut, out stdErr, environment, workingDir: testEnv.RootDir);

                var commandFilePath = "hello.cmd";
                bool isWindows = TestUtils.CurrentRuntimeEnvironment.OperatingSystem == "Windows";
                if (!isWindows)
                {
                    commandFilePath = "hello";
                }
                commandFilePath = Path.Combine(testEnv.RootDir, ".dnx/bin", commandFilePath);

                Assert.Equal(0, exitCode);
                Assert.True(string.IsNullOrEmpty(stdErr));
                Assert.True(File.Exists(commandFilePath));

                if (!isWindows)
                {
                    exitCode = TestUtils.Exec(commandFilePath, "", out stdOut, out stdErr);
                }
                else
                {
                    exitCode = TestUtils.Exec("cmd", $"/C {commandFilePath}", out stdOut, out stdErr);
                }
                Assert.Equal(0, exitCode);
                Assert.True(string.IsNullOrEmpty(stdErr));
                Assert.Contains("Write text", stdOut);
            }
        }
    }
}
