#region license
// Copyright 2026 Utah Department of Transportation
// for ReportApi - Utah.Udot.Atspm.ReportApi.ReportServices/LeftTurnSplitFailService.cs
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

using Utah.Udot.Atspm.Business.LeftTurnGapReport;

namespace Utah.Udot.Atspm.ReportApi.ReportServices
{
    /// <summary>
    /// Left turn gap analysis report service
    /// </summary>
    public class LeftTurnSplitFailService : ReportServiceBase<LeftTurnSplitFailOptions, LeftTurnSplitFailResult>
    {
        private readonly ILocationRepository locationRepository;
        private readonly IApproachSplitFailAggregationRepository approachSplitFailAggregationRepository;
        private readonly SplitFailService splitFailService;
        private readonly ILogger<LeftTurnSplitFailService> logger;

        /// <inheritdoc/>
        public LeftTurnSplitFailService(
            ILocationRepository locationRepository,
            IApproachSplitFailAggregationRepository approachSplitFailAggregationRepository,
            SplitFailService splitFailService,
            ILogger<LeftTurnSplitFailService> logger)
        {
            this.locationRepository = locationRepository;
            this.approachSplitFailAggregationRepository = approachSplitFailAggregationRepository;
            this.splitFailService = splitFailService;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public override async Task<LeftTurnSplitFailResult> ExecuteAsync(LeftTurnSplitFailOptions options, IProgress<int> progress = null, CancellationToken cancelToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(options.LocationIdentifier))
            {
                throw new ArgumentException("LocationIdentifier is required.", nameof(options.LocationIdentifier));
            }

            var location = locationRepository.GetLatestVersionOfLocation(options.LocationIdentifier, options.Start)
                ?? throw new InvalidOperationException($"Location '{options.LocationIdentifier}' was not found.");

            var approach = location.Approaches.FirstOrDefault(a => a.Id == options.ApproachId)
                ?? throw new InvalidOperationException(
                    $"Approach '{options.ApproachId}' was not found for location '{options.LocationIdentifier}'.");

            if (string.IsNullOrWhiteSpace(location.LocationIdentifier))
            {
                throw new InvalidOperationException(
                    $"Location '{options.LocationIdentifier}' is missing a valid LocationIdentifier.");
            }

            var splitfailaggregations = GetSplitFailAggregates(options, location.LocationIdentifier, approach);
            if (splitfailaggregations.Count == 0)
            {
                return new LeftTurnSplitFailResult
                {
                    CyclesWithSplitFails = 0,
                    SplitFailPercent = 0,
                    Direction = (approach.DirectionType?.Abbreviation ?? string.Empty)
                        + (approach.Detectors.FirstOrDefault()?.MovementType.ToString() ?? string.Empty)
                };
            }

            return splitFailService.GetSplitFailPercent(options, splitfailaggregations);
        }

        private List<ApproachSplitFailAggregation> GetSplitFailAggregates(
            LeftTurnSplitFailOptions options,
            string locationIdentifier,
            Approach approach)
        {

            var startTime = new TimeSpan(options.StartHour, options.StartMinute, 0);
            var endTime = new TimeSpan(options.EndHour, options.EndMinute, 0);
            List<ApproachSplitFailAggregation> splitFailsAggregates = new List<ApproachSplitFailAggregation>();
            for (var tempDate = options.Start.Date; tempDate <= options.End; tempDate = tempDate.AddDays(1))
            {
                if (options.DaysOfWeek?.Contains((int)tempDate.DayOfWeek) == true)
                {
                    splitFailsAggregates.AddRange(approachSplitFailAggregationRepository
                        .GetAggregationsBetweenDates(locationIdentifier, tempDate.Date.Add(startTime), tempDate.Date.Add(endTime))
                        .Where(a => a.ApproachId == approach.Id));
                }
            }

            return splitFailsAggregates;
        }


    }
}
