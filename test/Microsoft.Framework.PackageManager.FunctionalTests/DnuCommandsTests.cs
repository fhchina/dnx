// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.CommonTestUtils;
using Microsoft.Framework.Runtime.DependencyManagement;
using Xunit;

namespace Microsoft.Framework.PackageManager
{
    public class DnuCommandsTests
    {
        public static IEnumerable<object[]> RuntimeComponents
        {
            get
            {
                return TestUtils.GetRuntimeComponentsCombinations();
            }
        }

        private void InstallFakeApp(string rootDir, string name, string version)
        {
            Directory.CreateDirectory($"{rootDir}/packages/{name}/{version}/app");
            File.WriteAllText($"{rootDir}/packages/{name}/{version}/app/{name}.cmd", "");
            File.WriteAllText($"{rootDir}/{name}.cmd", $"~dp0/packages/{name}/{version}/app/{name}.cmd".Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText($"{rootDir}/packages/{name}/{version}/{name}.{version}.nupkg.sha512", "TestSha");
        }

        private void WriteLockFile(string dir, string libName, string version)
        {
            var lockFile = new LockFile
            {
                Islocked = false,
                Libraries = new List<LockFileLibrary>
                                {
                                    new LockFileLibrary
                                    {
                                        Name = libName,
                                        Version = new NuGet.SemanticVersion(version),
                                        Sha = "TestSha"
                                    }
                                }
            };
            var lockFormat = new LockFileFormat();
            lockFormat.Write($"{dir}/project.lock.json", lockFile);
        }

        [Theory]
        [MemberData("RuntimeComponents")]
        public void DnuCommands_Uninstall_PreservesPackagesUsedByOtherInstalledApps(string flavor, string os, string architecture)
        {
            var runtimeHomeDir = TestUtils.GetRuntimeHomeDir(flavor, os, architecture);
            using (var testEnv = new DnuTestEnvironment(runtimeHomeDir))
            {
                InstallFakeApp(testEnv.RootDir, "pack1", "0.0.0");
                InstallFakeApp(testEnv.RootDir, "pack2", "0.0.0");
                WriteLockFile($"{testEnv.RootDir}/packages/pack2/0.0.0/app", "pack2", "0.0.0");

                var repo = new AppCommandsFolderRepository(testEnv.RootDir);
                repo.Load();
                var reports = new Reports
                {
                    Error = new NullReport(),
                    Information = new NullReport(),
                    Quiet = new NullReport(),
                    Verbose = new NullReport()
                };
                var command = new UninstallCommand(repo, reports);

                command.Execute("pack1");

                //Pack2 is inuse by the pack2 app so should not be removed
                Assert.True(Directory.Exists($"{testEnv.RootDir}/packages/pack2"));
                //Pack1 only used by pack1 app so should be removed
                Assert.False(Directory.Exists($"{testEnv.RootDir}/packages/pack1"));
            }
        }
    }
}