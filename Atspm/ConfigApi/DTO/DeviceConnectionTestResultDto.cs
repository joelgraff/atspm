#region license
// Copyright 2026 Utah Department of Transportation
// for ConfigApi - Utah.Udot.Atspm.ConfigApi.DTO/DeviceConnectionTestResultDto.cs
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

namespace Utah.Udot.Atspm.ConfigApi.DTO
{
    /// <summary>
    /// Result payload for a device connection test.
    /// </summary>
    public class DeviceConnectionTestResultDto
    {
        /// <summary>
        /// True when connectivity and protocol checks succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Protocol used by the test.
        /// </summary>
        public string Protocol { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable summary of the test outcome.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Device identifier used for the test.
        /// </summary>
        public string DeviceIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Device IP address used for the test.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Port used for the test.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// OIDs attempted during the test.
        /// </summary>
        public List<string> OidsTried { get; set; } = [];

        /// <summary>
        /// OID values returned by the controller.
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = [];
    }
}