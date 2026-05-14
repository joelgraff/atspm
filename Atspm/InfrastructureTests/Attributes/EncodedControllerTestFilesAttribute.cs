#region license
// Copyright 2026 Utah Departement of Transportation
// for InfrastructureTests - Utah.Udot.Atspm.InfrastructureTests.Attributes/EncodedControllerTestFilesAttribute.cs
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

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Utah.Udot.Atspm.InfrastructureTests;
using Xunit.Sdk;

namespace Utah.Udot.Atspm.InfrastructureTests.Attributes
{
    public class EncodedControllerTestFilesAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new object[] { new FileInfo(TestDataPathHelper.EventLogDecoderTestData("4895_ECON_10.210.8.179_2024_02_21_1115.dat")), false, true };
            yield return new object[] { new FileInfo(TestDataPathHelper.EventLogDecoderTestData("1210_ECON_10.204.7.239_2021_08_09_1841.datZ")), true, true };
            yield return new object[] { new FileInfo(TestDataPathHelper.EventLogDecoderTestData("638548149067839806.xml")), false, false };
        }
    }
}
