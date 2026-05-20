using Microsoft.Extensions.Logging;
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
    public class PriorityAggregationBySignalTests
    {
        [Fact]
        public void LoadBins_PriorityServiceExtendedGreen_UsesExtendedGreenValues()
        {
            var priorityRepository = new Mock<IPriorityAggregationRepository>();
            var locationRepository = new Mock<ILocationRepository>();
            var logger = new Mock<ILogger<PriorityAggregationOptions>>();

            var start = new DateTime(2024, 1, 1, 0, 0, 0);
            var end = start.AddHours(1);

            var aggregationOptions = new AggregationOptions
            {
                DataType = (int)PriorityDataTypes.PriorityServiceExtendedGreen,
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

            var signal = new Location
            {
                LocationIdentifier = "1"
            };

            priorityRepository
                .Setup(r => r.GetAggregationsBetweenDates("1", start, end))
                .Returns(new List<PriorityAggregation>
                {
                    new PriorityAggregation
                    {
                        Start = start.AddMinutes(5),
                        PriorityServiceEarlyGreen = 2,
                        PriorityServiceExtendedGreen = 7
                    },
                    new PriorityAggregation
                    {
                        Start = start.AddMinutes(20),
                        PriorityServiceEarlyGreen = 3,
                        PriorityServiceExtendedGreen = 11
                    }
                });

            var metricOptions = new PriorityAggregationOptions(
                priorityRepository.Object,
                locationRepository.Object,
                logger.Object);

            var aggregationBySignal = new PriorityAggregationBySignal(
                metricOptions,
                signal,
                priorityRepository.Object,
                aggregationOptions);

            var firstBin = aggregationBySignal.BinsContainers.Single().Bins.Single();

            Assert.Equal(18, firstBin.Sum);
            Assert.Equal(18, firstBin.Average);
            priorityRepository.Verify(r => r.GetAggregationsBetweenDates("1", start, end), Times.Once);
        }
    }
}
