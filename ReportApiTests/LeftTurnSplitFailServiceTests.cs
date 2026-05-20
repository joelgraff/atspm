using Microsoft.Extensions.Logging;
using Moq;
using Utah.Udot.Atspm.Business.LeftTurnGapReport;
using Utah.Udot.Atspm.Data.Enums;
using Utah.Udot.Atspm.Data.Models;
using Utah.Udot.Atspm.Data.Models.MeasureOptions;
using Utah.Udot.Atspm.ReportApi.ReportServices;
using Utah.Udot.Atspm.Repositories.AggregationRepositories;
using Utah.Udot.Atspm.Repositories.ConfigurationRepositories;

namespace ReportApiTests
{
    public class LeftTurnSplitFailServiceTests
    {
        private readonly Mock<ILocationRepository> _locationRepository = new();
        private readonly Mock<IApproachSplitFailAggregationRepository> _splitFailAggregationRepository = new();
        private readonly Mock<ILogger<LeftTurnSplitFailService>> _logger = new();

        private LeftTurnSplitFailService CreateService()
        {
            var splitFailService = new SplitFailService(_locationRepository.Object);
            return new LeftTurnSplitFailService(
                _locationRepository.Object,
                _splitFailAggregationRepository.Object,
                splitFailService,
                _logger.Object);
        }

        private static LeftTurnSplitFailOptions CreateOptions()
        {
            var today = DateTime.Today;
            return new LeftTurnSplitFailOptions
            {
                LocationIdentifier = "LOC1",
                ApproachId = 100,
                Start = today,
                End = today,
                StartHour = 0,
                StartMinute = 0,
                EndHour = 23,
                EndMinute = 0,
                DaysOfWeek = new[] { (int)today.DayOfWeek }
            };
        }

        private static Location CreateLocation()
        {
            var approach = new Approach
            {
                Id = 100,
                DirectionType = new DirectionType { Abbreviation = "NB" },
                Detectors = new List<Detector>()
            };

            return new Location
            {
                LocationIdentifier = "LOC1",
                Approaches = new List<Approach> { approach }
            };
        }

        [Fact]
        public async Task ExecuteAsync_ValidOptions_ReturnsCalculatedSplitFailResult()
        {
            var service = CreateService();
            var options = CreateOptions();
            var location = CreateLocation();

            _locationRepository
                .Setup(r => r.GetLatestVersionOfLocation("LOC1", It.IsAny<DateTime>()))
                .Returns(location);

            _splitFailAggregationRepository
                .Setup(r => r.GetAggregationsBetweenDates("LOC1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<ApproachSplitFailAggregation>
                {
                    new ApproachSplitFailAggregation
                    {
                        LocationIdentifier = "LOC1",
                        ApproachId = 100,
                        Start = options.Start.AddHours(1),
                        End = options.Start.AddHours(1).AddMinutes(15),
                        Cycles = 10,
                        SplitFailures = 2
                    }
                });

            var result = await service.ExecuteAsync(options, progress: null, cancelToken: CancellationToken.None);

            Assert.Equal(2, result.CyclesWithSplitFails);
            Assert.Equal(20d, result.SplitFailPercent, 3);
            _splitFailAggregationRepository.Verify(
                r => r.GetAggregationsBetweenDates("LOC1", It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_MissingLocationIdentifier_ThrowsArgumentException()
        {
            var service = CreateService();
            var options = CreateOptions();
            options.LocationIdentifier = "";

            await Assert.ThrowsAsync<ArgumentException>(() => service.ExecuteAsync(options, progress: null, cancelToken: CancellationToken.None));

            _locationRepository.Verify(
                r => r.GetLatestVersionOfLocation(It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
            _splitFailAggregationRepository.Verify(
                r => r.GetAggregationsBetweenDates(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_NoAggregations_ReturnsZeroResultWithoutException()
        {
            var service = CreateService();
            var options = CreateOptions();
            var location = CreateLocation();

            _locationRepository
                .Setup(r => r.GetLatestVersionOfLocation("LOC1", It.IsAny<DateTime>()))
                .Returns(location);

            _splitFailAggregationRepository
                .Setup(r => r.GetAggregationsBetweenDates("LOC1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<ApproachSplitFailAggregation>());

            var result = await service.ExecuteAsync(options, progress: null, cancelToken: CancellationToken.None);

            Assert.Equal(0, result.CyclesWithSplitFails);
            Assert.Equal(0d, result.SplitFailPercent);
            Assert.Equal("NB", result.Direction);
        }
    }
}
