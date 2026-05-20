#region license
// Copyright 2026 Utah Department of Transportation
// for InfrastructureTests - Utah.Udot.Atspm.InfrastructureTests.RepositoryTests/ATSPMRepositoryEFBaseUpdateTrackingTests.cs
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

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Utah.Udot.Atspm.Data;
using Utah.Udot.Atspm.Data.Enums;
using Utah.Udot.Atspm.Data.Models;
using Utah.Udot.Atspm.Infrastructure.Repositories.ConfigurationRepositories;
using Xunit;

namespace Utah.Udot.Atspm.InfrastructureTests.RepositoryTests
{
    public class ATSPMRepositoryEFBaseUpdateTrackingTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ConfigContext _context;
        private readonly LocationEFRepository _locationRepository;

        public ATSPMRepositoryEFBaseUpdateTrackingTests()
        {
            _connection = new SqliteConnection("Datasource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ConfigContext>()
                .EnableSensitiveDataLogging()
                .UseSqlite(_connection)
                .Options;

            _context = new ConfigContext(options);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            if (!_context.LocationTypes.Any(l => l.Id == 1))
            {
                _context.LocationTypes.Add(new LocationType
                {
                    Id = 1,
                    Name = "Intersection"
                });
            }

            if (!_context.Regions.Any(r => r.Id == 1))
            {
                _context.Regions.Add(new Region
                {
                    Id = 1,
                    Description = "Test Region"
                });
            }

            if (!_context.Jurisdictions.Any(j => j.Id == 1))
            {
                _context.Jurisdictions.Add(new Jurisdiction
                {
                    Id = 1,
                    Name = "Test Jurisdiction"
                });
            }

            _context.SaveChanges();

            _locationRepository = new LocationEFRepository(_context, NullLogger<LocationEFRepository>.Instance);
        }

        [Fact]
        public void Update_DetachedEntity_UpdatesScalarPropertiesWithoutResettingRelationships()
        {
            var area = CreateArea("Area A");
            _context.Areas.Add(area);
            _context.SaveChanges();

            var location = CreateLocation("LOC-SCALAR", "Before", new[] { area });
            _locationRepository.Add(location);

            var detached = _context.Locations
                .AsNoTracking()
                .Include(l => l.Areas)
                .Single(l => l.Id == location.Id);

            detached.PrimaryName = "After";
            detached.Note = "Updated note";

            _locationRepository.Update(detached);

            var updated = _context.Locations
                .Include(l => l.Areas)
                .Single(l => l.Id == location.Id);

            Assert.Equal("After", updated.PrimaryName);
            Assert.Equal("Updated note", updated.Note);
            Assert.Single(updated.Areas);
            Assert.Equal(area.Id, updated.Areas.Single().Id);
        }

        [Fact]
        public void Update_DetachedEntity_UpdatesAreaNavigationMembership()
        {
            var areaA = CreateArea("Area A");
            var areaB = CreateArea("Area B");
            _context.AddRange(areaA, areaB);
            _context.SaveChanges();

            var location = CreateLocation("LOC-AREAS", "Area Test", new[] { areaA });
            _locationRepository.Add(location);

            var detached = _context.Locations
                .AsNoTracking()
                .Include(l => l.Areas)
                .Single(l => l.Id == location.Id);

            detached.Areas = new List<Area> { areaB };

            _locationRepository.Update(detached);

            var updated = _context.Locations
                .Include(l => l.Areas)
                .Single(l => l.Id == location.Id);

            Assert.Single(updated.Areas);
            Assert.Equal(areaB.Id, updated.Areas.Single().Id);
        }

        [Fact]
        public void UpdateRange_DetachedEntities_UpdatesAllItemsInSingleCall()
        {
            var locationA = CreateLocation("LOC-RANGE-A", "Before A");
            var locationB = CreateLocation("LOC-RANGE-B", "Before B");
            _locationRepository.AddRange(new[] { locationA, locationB });

            var detached = _context.Locations
                .AsNoTracking()
                .Where(l => l.LocationIdentifier == "LOC-RANGE-A" || l.LocationIdentifier == "LOC-RANGE-B")
                .OrderBy(l => l.LocationIdentifier)
                .ToList();

            detached[0].PrimaryName = "After A";
            detached[1].PrimaryName = "After B";

            _locationRepository.UpdateRange(detached);

            var updated = _context.Locations
                .AsNoTracking()
                .Where(l => l.LocationIdentifier == "LOC-RANGE-A" || l.LocationIdentifier == "LOC-RANGE-B")
                .OrderBy(l => l.LocationIdentifier)
                .Select(l => l.PrimaryName)
                .ToList();

            Assert.Equal(new[] { "After A", "After B" }, updated);
        }

        private static Area CreateArea(string name)
        {
            return new Area
            {
                Name = name
            };
        }

        private static Location CreateLocation(string locationIdentifier, string primaryName, IEnumerable<Area> areas = null)
        {
            return new Location
            {
                LocationIdentifier = locationIdentifier,
                PrimaryName = primaryName,
                SecondaryName = string.Empty,
                Latitude = 40.7608,
                Longitude = -111.8910,
                Note = "Initial",
                Start = DateTime.UtcNow,
                ChartEnabled = true,
                VersionAction = LocationVersionActions.Initial,
                PedsAre1to1 = false,
                LocationTypeId = 1,
                RegionId = 1,
                JurisdictionId = 1,
                Areas = areas?.ToList() ?? new List<Area>()
            };
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
    }
}
