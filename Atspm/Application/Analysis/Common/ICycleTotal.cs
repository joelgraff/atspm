#region license
// Copyright 2026 Utah Department of Transportation
// for Application - Utah.Udot.Atspm.Analysis.Common/ICycleTotal.cs
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

namespace Utah.Udot.Atspm.Analysis.Common
{
    /// <summary>
    /// Defines the start and end of a red to red cycle
    /// which is the time between two event code 9 events including
    /// event code 1 and event code 8
    /// </summary>
    public interface ICycleTotal : IStartEndRange
    {
        /// <summary>
        /// The total green time is defined as the time from start of event code 1 to the start of event code 8 in seconds
        /// </summary>
        double TotalGreenTime { get; }

        /// <summary>
        /// The total yellow time is defined as the time from start of event code 8 to the second event code 9 in seconds
        /// </summary>
        double TotalYellowTime { get; }

        /// <summary>
        /// The total red time is defined as the first event code 9 to the event code 1 in seconds
        /// </summary>
        double TotalRedTime { get; }

        /// <summary>
        /// The total time is defined as the time between the first and second event code 9 in seconds
        /// </summary>
        double TotalTime { get; }
    }
}
