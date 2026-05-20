#region license
// Copyright 2026 Utah Department of Transportation
// for ApplicationTests - Utah.Udot.Atspm.ApplicationTests.Analysis.WorkflowSteps/CalculateTotalVolumesTests.cs
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

using AutoFixture;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utah.Udot.Atspm.Analysis.Common;
using Utah.Udot.Atspm.Analysis.WorkflowSteps;
using Utah.Udot.Atspm.ApplicationTests.Analysis.TestObjects;
using Utah.Udot.Atspm.ApplicationTests.Fixtures;
using Utah.Udot.Atspm.Data.Enums;
using Utah.Udot.Atspm.Data.Models;
using Xunit;
using Xunit.Abstractions;

namespace Utah.Udot.Atspm.ApplicationTests.Analysis.WorkflowSteps
{
    public class CalculateTotalVolumesTests : IClassFixture<TestLocationFixture>, IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Location _testLocation;

        public CalculateTotalVolumesTests(ITestOutputHelper output, TestLocationFixture testLocation)
        {
            _output = output;
            _testLocation = testLocation.TestLocation;
        }

        private Volumes GenerateVolumes(string locationIdentifier, int phaseNumber, int detectorChannel, DirectionTypes direction, DateTime start, DateTime end, int count)
        {
            var correctedEventFixture = new Fixture();
            correctedEventFixture.Customize<CorrectedDetectorEvent>(c =>
            {
                return c.With(w => w.LocationIdentifier, locationIdentifier)
                .With(w => w.PhaseNumber, phaseNumber)
                .With(w => w.DetectorChannel, detectorChannel)
                .With(w => w.Direction, direction);
            });
            correctedEventFixture.Customizations.Add(new RandomDateTimeSequenceGenerator(start, end));

            var events = correctedEventFixture.CreateMany<CorrectedDetectorEvent>(count);

            var result = new Volumes(events, TimeSpan.FromMinutes(15))
            {
                LocationIdentifier = locationIdentifier,
                PhaseNumber = phaseNumber,
                Direction = direction,
            };

            result.Segments.ToList().ForEach(f =>
            {
                f.LocationIdentifier = locationIdentifier;
                f.PhaseNumber = phaseNumber;
                f.Direction = direction;
                f.DetectorEvents.AddRange(events.Where(w => f.InRange(w)));
            });

            return result;
        }

        private Volumes BuildVolumes(string locationIdentifier, int phaseNumber, int detectorChannel, DirectionTypes direction, params DateTime[] timestamps)
        {
            var events = timestamps.Select(ts => new CorrectedDetectorEvent
            {
                LocationIdentifier = locationIdentifier,
                PhaseNumber = phaseNumber,
                DetectorChannel = detectorChannel,
                Direction = direction,
                Timestamp = ts,
            }).ToList();

            var result = new Volumes(events, TimeSpan.FromMinutes(15))
            {
                LocationIdentifier = locationIdentifier,
                PhaseNumber = phaseNumber,
                Direction = direction,
            };

            result.Segments.ToList().ForEach(s =>
            {
                s.LocationIdentifier = locationIdentifier;
                s.PhaseNumber = phaseNumber;
                s.Direction = direction;
                s.DetectorEvents.AddRange(events.Where(e => s.InRange(e)));
            });

            return result;
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Location Filter")]
        public async Task CalculateTotalVolumesLocationFilterTest()
        {
            var start = DateTime.Parse("4/17/2023 8:00:00");
            var end = DateTime.Parse("4/17/2023 9:00:00");

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1001" },
                DirectionTypeId = DirectionTypes.EB,
            };

            var opposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1002" },
                DirectionTypeId = DirectionTypes.WB,
            };

            var primaryVolumes = GenerateVolumes("1001", 2, 2, DirectionTypes.EB, start, end, 3);
            var opposingVolumes = GenerateVolumes("1002", 6, 6, DirectionTypes.WB, start, end, 4);

            var sut = new CalculateTotalVolumes();
            var result = await sut.ExecuteAsync(Tuple.Create(
                Tuple.Create(primaryApproach, primaryVolumes),
                Tuple.Create(opposingApproach, opposingVolumes)));

            Assert.Null(result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Detector Filter")]
        public async Task CalculateTotalVolumesDetectorFilterTest()
        {
            var start = DateTime.Parse("4/17/2023 8:00:00");
            var end = DateTime.Parse("4/17/2023 9:00:00");

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1001" },
                DirectionTypeId = DirectionTypes.EB,
            };

            var nonOpposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1001" },
                DirectionTypeId = DirectionTypes.NB,
            };

            var primaryVolumes = GenerateVolumes("1001", 2, 2, DirectionTypes.EB, start, end, 3);
            var nonOpposingVolumes = GenerateVolumes("1001", 6, 6, DirectionTypes.NB, start, end, 4);

            var sut = new CalculateTotalVolumes();
            var result = await sut.ExecuteAsync(Tuple.Create(
                Tuple.Create(primaryApproach, primaryVolumes),
                Tuple.Create(nonOpposingApproach, nonOpposingVolumes)));

            Assert.Null(result.Item1);
            Assert.Null(result.Item2);
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Data Check")]
        public async Task CalculateTotalVolumesDataCheckTest()
        {
            var baseTime = DateTime.Parse("4/17/2023 8:00:00");

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1001" },
                DirectionTypeId = DirectionTypes.EB,
            };

            var opposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = "1001" },
                DirectionTypeId = DirectionTypes.WB,
            };

            var primaryEvents = new List<CorrectedDetectorEvent>
            {
                new CorrectedDetectorEvent { LocationIdentifier = "1001", PhaseNumber = 2, DetectorChannel = 2, Direction = DirectionTypes.EB, Timestamp = baseTime.AddMinutes(1) },
                new CorrectedDetectorEvent { LocationIdentifier = "1001", PhaseNumber = 2, DetectorChannel = 2, Direction = DirectionTypes.EB, Timestamp = baseTime.AddMinutes(16) },
            };

            var opposingEvents = new List<CorrectedDetectorEvent>
            {
                new CorrectedDetectorEvent { LocationIdentifier = "1001", PhaseNumber = 6, DetectorChannel = 6, Direction = DirectionTypes.WB, Timestamp = baseTime.AddMinutes(2) },
                new CorrectedDetectorEvent { LocationIdentifier = "1001", PhaseNumber = 6, DetectorChannel = 6, Direction = DirectionTypes.WB, Timestamp = baseTime.AddMinutes(20) },
                new CorrectedDetectorEvent { LocationIdentifier = "1001", PhaseNumber = 6, DetectorChannel = 6, Direction = DirectionTypes.WB, Timestamp = baseTime.AddMinutes(31) },
            };

            var primaryVolumes = new Volumes(primaryEvents, TimeSpan.FromMinutes(15))
            {
                LocationIdentifier = "1001",
                PhaseNumber = 2,
                Direction = DirectionTypes.EB,
            };

            primaryVolumes.Segments.ToList().ForEach(s =>
            {
                s.LocationIdentifier = "1001";
                s.PhaseNumber = 2;
                s.Direction = DirectionTypes.EB;
                s.DetectorEvents.AddRange(primaryEvents.Where(e => s.InRange(e)));
            });

            var opposingVolumes = new Volumes(opposingEvents, TimeSpan.FromMinutes(15))
            {
                LocationIdentifier = "1001",
                PhaseNumber = 6,
                Direction = DirectionTypes.WB,
            };

            opposingVolumes.Segments.ToList().ForEach(s =>
            {
                s.LocationIdentifier = "1001";
                s.PhaseNumber = 6;
                s.Direction = DirectionTypes.WB;
                s.DetectorEvents.AddRange(opposingEvents.Where(e => s.InRange(e)));
            });

            var sut = new CalculateTotalVolumes();
            var result = await sut.ExecuteAsync(Tuple.Create(
                Tuple.Create(primaryApproach, primaryVolumes),
                Tuple.Create(opposingApproach, opposingVolumes)));

            Assert.NotNull(result.Item1);
            Assert.NotNull(result.Item2);
            Assert.Equal("1001", result.Item2.LocationIdentifier);
            Assert.NotEmpty(result.Item2.Segments);
            Assert.Equal(0, result.Item2.DetectorCount);
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Start/End Check")]
        public async Task CalculatePhaseStartEndCheckTest()
        {
            var location = "1001";
            var baseTime = DateTime.Parse("4/17/2023 8:00:00");

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.EB,
            };

            var opposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.WB,
            };

            var primaryVolumes = BuildVolumes(location, 2, 2, DirectionTypes.EB,
                baseTime.AddMinutes(1),
                baseTime.AddMinutes(14),
                baseTime.AddMinutes(15),
                baseTime.AddMinutes(55));

            var opposingVolumes = BuildVolumes(location, 6, 6, DirectionTypes.WB,
                baseTime.AddMinutes(2),
                baseTime.AddMinutes(18),
                baseTime.AddMinutes(46));

            var sut = new CalculateTotalVolumes();
            var result = await sut.ExecuteAsync(Tuple.Create(
                Tuple.Create(primaryApproach, primaryVolumes),
                Tuple.Create(opposingApproach, opposingVolumes)));

            Assert.NotNull(result.Item2);
            Assert.Equal(DateTime.Parse("4/17/2023 8:00:00"), result.Item2.Start);
            Assert.Equal(DateTime.Parse("4/17/2023 9:00:00"), result.Item2.End);
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Time Segment Check")]
        public async Task CalculatePhaseTimeSegmentCheckTest()
        {
            var location = "1001";
            var baseTime = DateTime.Parse("4/17/2023 8:00:00");

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.EB,
            };

            var opposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.WB,
            };

            var primaryVolumes = BuildVolumes(location, 2, 2, DirectionTypes.EB,
                baseTime.AddMinutes(1),
                baseTime.AddMinutes(14),
                baseTime.AddMinutes(16),
                baseTime.AddMinutes(50),
                baseTime.AddMinutes(55));

            var opposingVolumes = BuildVolumes(location, 6, 6, DirectionTypes.WB,
                baseTime.AddMinutes(2),
                baseTime.AddMinutes(15),
                baseTime.AddMinutes(31),
                baseTime.AddMinutes(46));

            var sut = new CalculateTotalVolumes();
            var result = await sut.ExecuteAsync(Tuple.Create(
                Tuple.Create(primaryApproach, primaryVolumes),
                Tuple.Create(opposingApproach, opposingVolumes)));

            Assert.NotNull(result.Item2);
            Assert.Equal(4, result.Item2.Segments.Count);
            Assert.Equal(baseTime, result.Item2.Segments[0].Start);
            Assert.Equal(baseTime.AddMinutes(15), result.Item2.Segments[1].Start);
            Assert.Equal(baseTime.AddMinutes(30), result.Item2.Segments[2].Start);
            Assert.Equal(baseTime.AddMinutes(45), result.Item2.Segments[3].Start);
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "Null Input")]
        public async Task CalculateTotalVolumesNullInputTest()
        {
            var sut = new CalculateTotalVolumes();

            await Assert.ThrowsAsync<NullReferenceException>(async () =>
                await sut.ExecuteAsync(null));
        }

        [Fact]
        [Trait(nameof(CalculateTotalVolumes), "No Data")]
        public async Task CalculateTotalVolumesNoDataTest()
        {
            var location = "1001";

            var primaryApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.EB,
            };

            var opposingApproach = new Approach
            {
                Location = new Location { LocationIdentifier = location },
                DirectionTypeId = DirectionTypes.WB,
            };

            var primaryVolumes = BuildVolumes(location, 2, 2, DirectionTypes.EB);
            var opposingVolumes = BuildVolumes(location, 6, 6, DirectionTypes.WB);

            var sut = new CalculateTotalVolumes();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.ExecuteAsync(Tuple.Create(
                    Tuple.Create(primaryApproach, primaryVolumes),
                    Tuple.Create(opposingApproach, opposingVolumes))));
        }

        [Theory]
        [InlineData("CalculateTotalVolumesTestData1.json")]
        [Trait(nameof(CalculateTotalVolumes), "From File")]
        public async Task CalculateTotalVolumesFromFileTest(string file)
        {
            var path = TestDataPathHelper.ApplicationAnalysisTestData(file);
            var json = File.ReadAllText(new FileInfo(path).FullName);
            var testFile = JsonConvert.DeserializeObject<CalculateTotalVolumeTestData>(json);

            _output.WriteLine($"Configuration: {testFile.Configuration}");
            _output.WriteLine($"Input: {testFile.Input.Count}");
            _output.WriteLine($"Output: {testFile.Output.Segments.Count}");

            var t1 = Tuple.Create(testFile.Configuration[0], testFile.Input[0]);
            var t2 = Tuple.Create(testFile.Configuration[1], testFile.Input[1]);

            var testData = Tuple.Create(t1, t2);

            var sut = new CalculateTotalVolumes();

            var result = await sut.ExecuteAsync(testData);

            var expected = testFile.Output;
            var actual = result.Item2;

            Assert.NotNull(result.Item1);
            Assert.NotNull(actual);
            Assert.Equal(testFile.Configuration[0].Location.LocationIdentifier, actual.LocationIdentifier);
            Assert.True(actual.Segments.Count > 0);
            Assert.Equal(expected.Start, actual.Start);
            Assert.Equal(expected.End, actual.End);
        }

        public void Dispose()
        {
        }
    }
}
