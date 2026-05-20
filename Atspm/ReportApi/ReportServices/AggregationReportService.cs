#region license
// Copyright 2026 Utah Department of Transportation
// for ReportApi - Utah.Udot.Atspm.ReportApi.ReportServices/AggregationReportService.cs
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

using Utah.Udot.Atspm.ReportApi.DataAggregation;

namespace Utah.Udot.Atspm.ReportApi.ReportServices
{


    /// <summary>
    /// Approach delay report service
    /// </summary>
    public class AggregationReportService : ReportServiceBase<AggregationOptions, IEnumerable<AggregationResult>>
    {
        private readonly ILocationRepository locationRepository;
        private readonly DetectorVolumeAggregationOptions detectorVolumeAggregationOptions;
        private readonly ApproachSpeedAggregationOptions approachSpeedAggregationOptions;
        private readonly ApproachPcdAggregationOptions approachPcdAggregationOptions;
        private readonly PhaseCycleAggregationOptions phaseCycleAggregationOptions;
        private readonly ApproachSplitFailAggregationOptions approachSplitFailAggregationOptions;
        private readonly ApproachYellowRedActivationsAggregationOptions approachYellowRedActivationsAggregationOptions;
        private readonly PreemptionAggregationOptions preemptionAggregationOptions;
        private readonly PriorityAggregationOptions priorityAggregationOptions;
        private readonly SignalEventCountAggregationOptions signalEventCountAggregationOptions;
        private readonly PhaseTerminationAggregationOptions phaseTerminationAggregationOptions;
        private readonly PhasePedAggregationOptions phasePedAggregationOptions;
        private readonly PhaseLeftTurnGapAggregationOptions phaseLeftTurnGapAggregationOptions;
        private readonly PhaseSplitMonitorAggregationOptions phaseSplitMonitorAggregationOptions;
        private readonly IDetectorEventCountAggregationRepository detectorEventCountAggregationRepository;
        private readonly ILogger<AggregationReportService> logger;

        /// <inheritdoc/>
        public AggregationReportService(
            ILocationRepository locationRepository,
            DetectorVolumeAggregationOptions detectorVolumeAggregationOptions,
            ApproachSpeedAggregationOptions approachSpeedAggregationOptions,
            ApproachPcdAggregationOptions approachPcdAggregationOptions,
            PhaseCycleAggregationOptions phaseCycleAggregationOptions,
            ApproachSplitFailAggregationOptions approachSplitFailAggregationOptions,
            ApproachYellowRedActivationsAggregationOptions approachYellowRedActivationsAggregationOptions,
            PreemptionAggregationOptions preemptionAggregationOptions,
            PriorityAggregationOptions priorityAggregationOptions,
            SignalEventCountAggregationOptions signalEventCountAggregationOptions,
            PhaseTerminationAggregationOptions phaseTerminationAggregationOptions,
            PhasePedAggregationOptions phasePedAggregationOptions,
            PhaseLeftTurnGapAggregationOptions phaseLeftTurnGapAggregationOptions,
            PhaseSplitMonitorAggregationOptions phaseSplitMonitorAggregationOptions,
            IDetectorEventCountAggregationRepository detectorEventCountAggregationRepository,
            ILogger<AggregationReportService> logger
            )
        {
            this.locationRepository = locationRepository;
            this.detectorVolumeAggregationOptions = detectorVolumeAggregationOptions;
            this.approachSpeedAggregationOptions = approachSpeedAggregationOptions;
            this.approachPcdAggregationOptions = approachPcdAggregationOptions;
            this.phaseCycleAggregationOptions = phaseCycleAggregationOptions;
            this.approachSplitFailAggregationOptions = approachSplitFailAggregationOptions;
            this.approachYellowRedActivationsAggregationOptions = approachYellowRedActivationsAggregationOptions;
            this.preemptionAggregationOptions = preemptionAggregationOptions;
            this.priorityAggregationOptions = priorityAggregationOptions;
            this.signalEventCountAggregationOptions = signalEventCountAggregationOptions;
            this.phaseTerminationAggregationOptions = phaseTerminationAggregationOptions;
            this.phasePedAggregationOptions = phasePedAggregationOptions;
            this.phaseLeftTurnGapAggregationOptions = phaseLeftTurnGapAggregationOptions;
            this.phaseSplitMonitorAggregationOptions = phaseSplitMonitorAggregationOptions;
            this.detectorEventCountAggregationRepository = detectorEventCountAggregationRepository;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<AggregationResult>> ExecuteAsync(AggregationOptions options, IProgress<int>? progress = null, CancellationToken cancelToken = default)
        {
            switch (options.AggregationType)
            {
                case AggregationType.DetectorEventCount:
                    return Task.FromResult<IEnumerable<AggregationResult>>(detectorVolumeAggregationOptions.CreateMetric(options));
                case AggregationType.Speed:
                    return Task.FromResult<IEnumerable<AggregationResult>>(approachSpeedAggregationOptions.CreateMetric(options));
                case AggregationType.Pcd:
                    return Task.FromResult<IEnumerable<AggregationResult>>(approachPcdAggregationOptions.CreateMetric(options));
                case AggregationType.PhaseCycle:
                    return Task.FromResult<IEnumerable<AggregationResult>>(phaseCycleAggregationOptions.CreateMetric(options));
                case AggregationType.SplitFail:
                    return Task.FromResult<IEnumerable<AggregationResult>>(approachSplitFailAggregationOptions.CreateMetric(options));
                case AggregationType.YellowRedActivation:
                    return Task.FromResult<IEnumerable<AggregationResult>>(approachYellowRedActivationsAggregationOptions.CreateMetric(options));
                case AggregationType.Preemption:
                    return Task.FromResult<IEnumerable<AggregationResult>>(preemptionAggregationOptions.CreateMetric(options));
                case AggregationType.Priority:
                    return Task.FromResult<IEnumerable<AggregationResult>>(priorityAggregationOptions.CreateMetric(options));
                case AggregationType.SignalEventCount:
                    return Task.FromResult<IEnumerable<AggregationResult>>(signalEventCountAggregationOptions.CreateMetric(options));
                case AggregationType.PhaseTermination:
                    return Task.FromResult<IEnumerable<AggregationResult>>(phaseTerminationAggregationOptions.CreateMetric(options));
                case AggregationType.Ped:
                    return Task.FromResult<IEnumerable<AggregationResult>>(phasePedAggregationOptions.CreateMetric(options));
                case AggregationType.PhaseLeftTurn:
                    return Task.FromResult<IEnumerable<AggregationResult>>(phaseLeftTurnGapAggregationOptions.CreateMetric(options));
                case AggregationType.SplitMonitor:
                    return Task.FromResult<IEnumerable<AggregationResult>>(phaseSplitMonitorAggregationOptions.CreateMetric(options));
                default:
                    throw new Exception("Unknown Chart Type");
            }

        }

        //private ActionResult GetLaneByLaneChart(AggregationOptions aggDataExportViewModel)
        //{
        //    return GetChart(aggDataExportViewModel, options);
        //}

        //private void SetLocations(AggregationOptions options, List<Location> locations)
        //{
        //    foreach (var LocationIdentifier in options.LocationIdentifiers)
        //    {
        //        var location = locationRepository.GetLatestVersionOfLocation(LocationIdentifier, options.Start);
        //        if (location != null)
        //        {
        //            locations.Add(location);
        //        }
        //    }
        //}
    }
}
