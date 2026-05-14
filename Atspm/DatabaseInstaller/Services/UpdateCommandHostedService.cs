#region license
// Copyright 2026 Utah Department of Transportation
// for DatabaseInstaller - DatabaseInstaller.Services/UpdateCommandHostedService.cs
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

using global::DatabaseInstaller.Commands;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utah.Udot.Atspm.Common;
using Utah.Udot.Atspm.Data;
using Utah.Udot.Atspm.Data.Models.IdentityModels;


namespace DatabaseInstaller.Services
{
    public class UpdateCommandHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly UpdateCommandConfiguration _config;
        private readonly ILogger<UpdateCommandHostedService> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public UpdateCommandHostedService(
            IServiceProvider serviceProvider,
            IOptions<UpdateCommandConfiguration> config,
            ILogger<UpdateCommandHostedService> logger,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _config = config.Value;
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Apply migrations
                await ApplyMigrationsForAllContexts(cancellationToken);

                // Optionally seed admin
                if (_config.SeedAdmin)
                {
                    await SeedAdminUserAndAssignRole();
                }

                _logger.LogInformation("Database migration and admin seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception during migration or seeding: {Exception}", ex);
            }
            finally
            {
                _logger.LogInformation("Shutting down the application after completion.");
                _hostApplicationLifetime.StopApplication(); // Stop the host after all tasks are complete
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ApplyMigrationsForAllContexts(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // ConfigContext
            var configContext = serviceProvider.GetRequiredService<ConfigContext>();
            if (!string.IsNullOrEmpty(_config.ConfigConnection))
            {
                _logger.LogInformation("Overriding ConfigContext connection string.");
                configContext.Database.SetConnectionString(_config.ConfigConnection);
            }
            _logger.LogInformation("Applying migrations for ConfigContext.");
            await configContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations applied for ConfigContext.");

            // AggregationContext
            var aggregationContext = serviceProvider.GetRequiredService<AggregationContext>();
            if (!string.IsNullOrEmpty(_config.AggregationConnection))
            {
                _logger.LogInformation("Overriding AggregationContext connection string.");
                aggregationContext.Database.SetConnectionString(_config.AggregationConnection);
            }
            _logger.LogInformation("Applying migrations for AggregationContext.");
            await aggregationContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations applied for AggregationContext.");

            // EventLogContext
            var eventLogContext = serviceProvider.GetRequiredService<EventLogContext>();
            if (!string.IsNullOrEmpty(_config.EventLogConnection))
            {
                _logger.LogInformation("Overriding EventLogContext connection string.");
                eventLogContext.Database.SetConnectionString(_config.EventLogConnection);
            }
            _logger.LogInformation("Applying migrations for EventLogContext.");
            await eventLogContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations applied for EventLogContext.");

            // IdentityContext
            var identityContext = serviceProvider.GetRequiredService<IdentityContext>();
            if (!string.IsNullOrEmpty(_config.IdentityConnection))
            {
                _logger.LogInformation("Overriding IdentityContext connection string.");
                identityContext.Database.SetConnectionString(_config.IdentityConnection);
            }
            _logger.LogInformation("Applying migrations for IdentityContext.");
            await identityContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Migrations applied for IdentityContext.");
        }

        private async Task SeedAdminUserAndAssignRole()
        {
            using var scope = _serviceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            if (!string.IsNullOrEmpty(_config.IdentityConnection))
                identityContext.Database.SetConnectionString(_config.IdentityConnection);
            }

            // Ensure the database is migrated before proceeding
            await identityContext.Database.MigrateAsync();

            // Manually create the dependencies required for UserManager<ApplicationUser>
            var userStore = new UserStore<ApplicationUser>(identityContext);
            var roleStore = new RoleStore<IdentityRole>(identityContext);
            var passwordHasher = new PasswordHasher<ApplicationUser>();

            // Explicitly configure IdentityOptions to allow the provided password
            var identityOptions = new IdentityOptions();
            identityOptions.Password.RequireDigit = true;
            identityOptions.Password.RequiredLength = 6;
            identityOptions.Password.RequireLowercase = true;
            identityOptions.Password.RequireUppercase = true;
            identityOptions.Password.RequireNonAlphanumeric = true;
            identityOptions.Password.RequiredUniqueChars = 1;

            var options = Options.Create(identityOptions);

            var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
            var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
            var roleValidators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
            var keyNormalizer = serviceProvider.GetRequiredService<ILookupNormalizer>();
            var errors = serviceProvider.GetRequiredService<IdentityErrorDescriber>();
            var logger = serviceProvider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>();
            var roleLogger = serviceProvider.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

            // Manually create UserManager with configured password options
            var userManager = new UserManager<ApplicationUser>(
                userStore, options, passwordHasher, userValidators, passwordValidators,
                keyNormalizer, errors, serviceProvider, logger);
            var roleManager = new RoleManager<IdentityRole>(
                roleStore, roleValidators, keyNormalizer, errors, roleLogger);

            // Ensure the admin role exists and has the minimum claim required by auth policies.
            if (!await roleManager.RoleExistsAsync(_config.AdminRole))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole(_config.AdminRole));
                if (!createRoleResult.Succeeded)
                {
                    _logger.LogError("Failed to create admin role: {Errors}",
                        string.Join(", ", createRoleResult.Errors.Select(e => e.Description)));
                    return;
                }
            }

            var seededRole = await roleManager.FindByNameAsync(_config.AdminRole);
            if (seededRole != null)
            {
                var adminClaimExists = await identityContext.Set<IdentityRoleClaim<string>>()
                    .AnyAsync(c =>
                        c.RoleId == seededRole.Id &&
                        c.ClaimType == AtspmAuthorization.RoleClaimType &&
                        c.ClaimValue == AtspmAuthorization.Permissions.Admin);

                if (!adminClaimExists)
                {
                    identityContext.Set<IdentityRoleClaim<string>>().Add(new IdentityRoleClaim<string>
                    {
                        RoleId = seededRole.Id,
                        ClaimType = AtspmAuthorization.RoleClaimType,
                        ClaimValue = AtspmAuthorization.Permissions.Admin,
                    });

                    await identityContext.SaveChangesAsync();
                }
            }

            // Check if the admin user already exists.
            var adminUser = await userManager.FindByEmailAsync(_config.AdminEmail);
            if (adminUser == null)
            {
                // Create the admin user.
                adminUser = new ApplicationUser
                {
                    UserName = _config.AdminEmail,
                    Email = _config.AdminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "Admin",
                    Agency = "Transportation Agency",
                };

                var createResult = await userManager.CreateAsync(adminUser, _config.AdminPassword);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create admin user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                _logger.LogInformation("Admin user created successfully.");
            }
            else
            {
                _logger.LogInformation("Admin user already exists.");
            }

            // Assign the Admin role to the user if not already assigned.
            if (!await userManager.IsInRoleAsync(adminUser, _config.AdminRole))
            {
                var roleResult = await userManager.AddToRoleAsync(adminUser, _config.AdminRole);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to assign admin role: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    return;
                }
                _logger.LogInformation("Admin user assigned to Admin role successfully.");
            }
            else
            {
                _logger.LogInformation("Admin user is already assigned to the Admin role.");
            }
        }






    }


}
