#region license
// Copyright 2026 Utah Departement of Transportation
// for InfrastructureTests - Utah.Udot.Atspm.InfrastructureTests/TestDataPathHelper.cs
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.IO;

namespace Utah.Udot.Atspm.InfrastructureTests
{
    internal static class TestDataPathHelper
    {
        public static string EventLogDecoderTestData(string fileName)
        {
            var root = FindRepoRoot();
            return Path.Combine(root, "InfrastructureTests", "EventLogDecoderTests", "TestData", fileName);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ATSPM.sln")))
            {
                dir = dir.Parent;
            }

            if (dir is null)
            {
                throw new DirectoryNotFoundException("Could not locate repository root containing ATSPM.sln.");
            }

            return dir.FullName;
        }
    }
}