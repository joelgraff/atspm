using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Utah.Udot.Atspm.Business.Aggregation;
using Utah.Udot.Atspm.Business.Bins;
using Utah.Udot.Atspm.Data.Enums;
using Utah.Udot.Atspm.Data.Models;
using Utah.Udot.Atspm.Enums;
using Utah.Udot.Atspm.ReportApi.DataAggregation;
using Utah.Udot.Atspm.Repositories.AggregationRepositories;
using Utah.Udot.Atspm.Repositories.ConfigurationRepositories;

namespace ReportApiTests
{
    public class SignalAggregationApproachOverloadTests
    {
        [Fact]
        public void PriorityAggregation_ApproachLoadBins_DoesNotThrowAndAggregates()
        {
            var priorityRepository = new Mock<IPriorityAggregationRepository>();
            var locationRepository = new Mock<ILocationRepository>();
            var logger = new Mock<ILogger<PriorityAggregationOptions>>();

            var start = new DateTime(2024, 1, 1, 0, 0, 0);
            var end = start.AddHours(1);
            var options = CreateAggregationOptions(start, end, (int)PriorityDataTypes.PriorityRequests);
            var signal = CreateSignal();

            priorityRepository
                .Setup(r => r.GetAggregationsBetweenDates("1", start, end))
                .Returns(new List<PriorityAggregation>
                {
                    new PriorityAggregation { Start = start.AddMinutes(5), PriorityRequests = 3 },
                    new PriorityAggregation { Start = start.AddMinutes(25), PriorityRequests = 4 }
                });

            var signalOptions = new PriorityAggregationOptions(priorityRepository.Object, locationRepository.Object, logger.Object);
            var approachOptions = new DummyApproachAggregationMetricOptions(locationRepository.Object);

            var aggregation = new TestablePriorityAggregationBySignal(signalOptions, signal, priorityRepository.Object, options);

            aggregation.InvokeApproachLoadBins(approachOptions, signal, options);

            Assert.Equal(7, aggregation.BinsContainers.Single().Bins.Single().Sum);
        }

        [Fact]
        public void PreemptionAggregation_ApproachLoadBins_DoesNotThrowAndAggregates()
        {
            var preemptionRepository = new Mock<IPreemptionAggregationRepository>();
            var locationRepository = new Mock<ILocationRepository>();
            var logger = new Mock<ILogger<PhaseTerminationAggregationOptions>>();

            var start = new DateTime(2024, 1, 1, 0, 0, 0);
            var end = start.AddHours(1);
            var options = CreateAggregationOptions(start, end, (int)PreemptionDataTypes.PreemptServices);
            var signal = CreateSignal();

            preemptionRepository
                .Setup(r => r.GetAggregationsBetweenDates("1", start, end))
                .Returns(new List<PreemptionAggregation>
                {
                    new PreemptionAggregation { Start = start.AddMinutes(6), PreemptServices = 2 },
                    new PreemptionAggregation { Start = start.AddMinutes(16), PreemptServices = 8 }
                });

            var signalOptions = new PreemptionAggregationOptions(preemptionRepository.Object, locationRepository.Object, logger.Object);
            var approachOptions = new DummyApproachAggregationMetricOptions(locationRepository.Object);

            var aggregation = new TestablePreemptionAggregationBySignal(signalOptions, signal, preemptionRepository.Object, options);

            aggregation.InvokeApproachLoadBins(approachOptions, signal, options);

            Assert.Equal(10, aggregation.BinsContainers.Single().Bins.Single().Sum);
        }

        [Fact]
        public void SignalEventCountAggregation_ApproachLoadBins_DoesNotThrowAndAggregates()
        {
            var eventCountRepository = new Mock<ISignalEventCountAggregationRepository>();
            var locationRepository = new Mock<ILocationRepository>();
            var logger = new Mock<ILogger<PhaseTerminationAggregationOptions>>();

            var start = new DateTime(2024, 1, 1, 0, 0, 0);
            var end = start.AddHours(1);
            var options = CreateAggregationOptions(start, end, (int)SignalEventCountDataTypes.EventCount);
            var signal = CreateSignal();

            eventCountRepository
                .Setup(r => r.GetAggregationsBetweenDates("1", start, end))
                .Returns(new List<SignalEventCountAggregation>
                {
                    new SignalEventCountAggregation { Start = start.AddMinutes(10), EventCount = 5 },
                    new SignalEventCountAggregation { Start = start.AddMinutes(20), EventCount = 6 }
                });

            var signalOptions = new SignalEventCountAggregationOptions(eventCountRepository.Object, locationRepository.Object, logger.Object);
            var approachOptions = new DummyApproachAggregationMetricOptions(locationRepository.Object);

            var aggregation = new TestableSignalEventCountAggregationBySignal(signalOptions, signal, eventCountRepository.Object, options);

            aggregation.InvokeApproachLoadBins(approachOptions, signal, options);

            Assert.Equal(11, aggregation.BinsContainers.Single().Bins.Single().Sum);
        }

        private static AggregationOptions CreateAggregationOptions(DateTime start, DateTime end, int dataType)
        {
            return new AggregationOptions
            {
                DataType = dataType,
                SelectedAggregationType = AggregationCalculationType.Sum,
                SelectedXAxisType = XAxisType.Time,
                SelectedSeries = SeriesType.Signal,
                TimeOptions = new TimeOptions
                {
                    Start = start,
                    End = end,
                    TimeOption = TimeOptions.TimePeriodOptions.StartToEnd,
                    SelectedBinSize = TimeOptions.BinSize.Hour,
                    DaysOfWeek = new List<DayOfWeek>
                    {
                        DayOfWeek.Monday,
                        DayOfWeek.Tuesday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Thursday,
                        DayOfWeek.Friday,
                        DayOfWeek.Saturday,
                        DayOfWeek.Sunday
                    }
                }
            };
        }

        private static Location CreateSignal()
        {
            return new Location { LocationIdentifier = "1" };
        }

        private sealed class DummyApproachAggregationMetricOptions : ApproachAggregationMetricOptions
        {
            public DummyApproachAggregationMetricOptions(ILocationRepository locationRepository)
                : base(locationRepository, NullLogger<DummyApproachAggregationMetricOptions>.Instance)
            {
            }

            protected override List<BinsContainer> GetBinsContainersByApproach(Approach approach, bool getprotectedPhase, AggregationOptions options) => new();
            protected override int GetAverageByPhaseNumber(Location signal, int phaseNumber, AggregationOptions options) => 0;
            protected override double GetSumByPhaseNumber(Location signal, int phaseNumber, AggregationOptions options) => 0;
            protected override int GetAverageByDirection(Location signal, DirectionTypes direction, AggregationOptions options) => 0;
            protected override double GetSumByDirection(Location signal, DirectionTypes direction, AggregationOptions options) => 0;
            protected override List<BinsContainer> GetBinsContainersByDirection(DirectionTypes directionType, Location signal, AggregationOptions options) => new();
            protected override List<BinsContainer> GetBinsContainersByPhaseNumber(Location signal, int phaseNumber, AggregationOptions options) => new();
            protected override List<BinsContainer> GetBinsContainersBySignal(Location signal, AggregationOptions options) => new();
            public override List<BinsContainer> GetBinsContainersByRoute(List<Location> signals, AggregationOptions options) => new();
        }

        private sealed class TestablePriorityAggregationBySignal : PriorityAggregationBySignal
        {
            public TestablePriorityAggregationBySignal(
                PriorityAggregationOptions priorityAggregationOptions,
                Location signal,
                IPriorityAggregationRepository priorityAggregationRepository,
                AggregationOptions options)
                : base(priorityAggregationOptions, signal, priorityAggregationRepository, options)
            {
            }

            public void InvokeApproachLoadBins(ApproachAggregationMetricOptions approachOptions, Location signal, AggregationOptions options)
            {
                base.LoadBins(approachOptions, signal, options);
            }
        }

        private sealed class TestablePreemptionAggregationBySignal : PreemptionAggregationBySignal
        {
            public TestablePreemptionAggregationBySignal(
                PreemptionAggregationOptions preemptionAggregationOptions,
                Location signal,
                IPreemptionAggregationRepository preemptionAggregationRepository,
                AggregationOptions options)
                : base(preemptionAggregationOptions, signal, preemptionAggregationRepository, options)
            {
            }

            public void InvokeApproachLoadBins(ApproachAggregationMetricOptions approachOptions, Location signal, AggregationOptions options)
            {
                base.LoadBins(approachOptions, signal, options);
            }
        }

        private sealed class TestableSignalEventCountAggregationBySignal : SignalEventCountAggregationBySignal
        {
            public TestableSignalEventCountAggregationBySignal(
                SignalEventCountAggregationOptions signalEventCountAggregation,
                Location signal,
                ISignalEventCountAggregationRepository signalEventCountAggregationRepository,
                AggregationOptions options)
                : base(signalEventCountAggregation, signal, signalEventCountAggregationRepository, options)
            {
            }

            public void InvokeApproachLoadBins(ApproachAggregationMetricOptions approachOptions, Location signal, AggregationOptions options)
            {
                base.LoadBins(approachOptions, signal, options);
            }
        }
    }
}
