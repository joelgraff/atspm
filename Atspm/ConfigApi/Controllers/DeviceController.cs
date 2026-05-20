#region license
// Copyright 2026 Utah Department of Transportation
// for ConfigApi - Utah.Udot.Atspm.ConfigApi.Controllers/DeviceController.cs
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

using Asp.Versioning;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Utah.Udot.Atspm.Common;
using Utah.Udot.Atspm.ConfigApi.DTO;
using Utah.Udot.Atspm.Data;
using Utah.Udot.Atspm.Data.Enums;
using Utah.Udot.Atspm.Data.Models;
using Utah.Udot.Atspm.Infrastructure.Attributes;
using Utah.Udot.Atspm.Repositories.ConfigurationRepositories;
using Utah.Udot.ATSPM.ConfigApi.Models;
using static Microsoft.AspNetCore.Http.StatusCodes;
using static Microsoft.AspNetCore.OData.Query.AllowedQueryOptions;

namespace Utah.Udot.Atspm.ConfigApi.Controllers
{
    /// <summary>
    /// Device controller
    /// </summary>
    [ApiVersion(1.0)]
    public class DeviceController(IDeviceRepository repository, ConfigContext configContext) : DevicePolicyControllerBase<Device, int>(repository)
    {
        private static readonly Regex OidRegex = new(@"^\d+(\.\d+)+$", RegexOptions.Compiled);
        private readonly IDeviceRepository _repository = repository;
        private readonly ConfigContext _configContext = configContext;

        #region NavigationProperties

        #endregion

        #region Actions

        /// <summary>
        /// Tests controller connectivity for a specific location device.
        /// Current implementation supports SNMP configurations.
        /// </summary>
        /// <param name="key">Device ID</param>
        /// <returns>Connection test result</returns>
        [HttpGet("api/v{version:apiVersion}/Device/{key}/TestConnection")]
        [AuthorizePermission(AtspmAuthorization.Permissions.DeviceView)]
        [ProducesResponseType(typeof(DeviceConnectionTestResultDto), Status200OK)]
        [ProducesResponseType(typeof(DeviceConnectionTestResultDto), Status400BadRequest)]
        [ProducesResponseType(Status404NotFound)]
        public async Task<IActionResult> TestConnection([FromRoute] int key)
        {
            var device = await _configContext.Devices
                .Include(d => d.DeviceConfiguration)
                .FirstOrDefaultAsync(d => d.Id == key);

            if (device is null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(device.Ipaddress))
            {
                return BadRequest(new DeviceConnectionTestResultDto
                {
                    Success = false,
                    DeviceIdentifier = device.DeviceIdentifier ?? string.Empty,
                    Message = "Device does not have an IP address configured."
                });
            }

            if (device.DeviceConfiguration is null)
            {
                return BadRequest(new DeviceConnectionTestResultDto
                {
                    Success = false,
                    DeviceIdentifier = device.DeviceIdentifier ?? string.Empty,
                    IpAddress = device.Ipaddress,
                    Message = "Device does not have a device configuration assigned."
                });
            }

            var config = device.DeviceConfiguration;

            if (config.Protocol != TransportProtocols.Snmp)
            {
                return BadRequest(new DeviceConnectionTestResultDto
                {
                    Success = false,
                    Protocol = config.Protocol.ToString(),
                    DeviceIdentifier = device.DeviceIdentifier ?? string.Empty,
                    IpAddress = device.Ipaddress,
                    Port = config.Port,
                    Message = "Live connection testing is currently implemented for SNMP protocol only."
                });
            }

            return await TestSnmpConnection(config, device);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Gets all <see cref="Device"/> from <paramref name="locationId"/> where <see cref="Device.DeviceStatus"/> equals <see cref="DeviceStatus.Active"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = Count | Filter | Select | OrderBy | Top | Skip)]
        [ProducesResponseType(typeof(IEnumerable<Device>), Status200OK)]
        public IActionResult GetActiveDevicesByLocation(int locationId)
        {
            return Ok(_repository.GetActiveDevicesByLocation(locationId));
        }

        /// <summary>
        /// Gets a count of device type for all active devices <see cref="Device"/> where <see cref="Device.DeviceStatus"/> equals <see cref="DeviceStatus.Active"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [EnableQuery(AllowedQueryOptions = Count | Filter | Select | OrderBy | Top | Skip)]
        [ProducesResponseType(typeof(IEnumerable<DeviceGroup>), Status200OK)]
        public IActionResult GetActiveDevicesCount()
        {
            var devices = _repository.GetActiveDevicesByAllLatestLocations();
            var deviceGroups = devices.GroupBy(d => new
            {
                Manufacturer = d.DeviceConfiguration.Product.Manufacturer,
                Model = d.DeviceConfiguration.Product.Model,
                Firmware = d.DeviceConfiguration.Description
            })
                //HACK: This is a dto not a model
                .Select(g => new ATSPM.ConfigApi.Models.DeviceGroup
                {
                    Manufacturer = g.Key.Manufacturer,
                    Model = g.Key.Model,
                    Firmware = g.Key.Firmware,
                    Count = g.Count()
                })
                .ToList();

            return Ok(deviceGroups);
        }

        private async Task<IActionResult> TestSnmpConnection(DeviceConfiguration config, Device device)
        {
            var oids = (config.Query ?? [])
                .Where(q => !string.IsNullOrWhiteSpace(q))
                .Select(q => q.Trim())
                .Where(q => OidRegex.IsMatch(q))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (oids.Count == 0)
            {
                oids.Add("1.3.6.1.2.1.1.1.0");
            }

            var community = GetConnectionProperty(config.ConnectionProperties, "Community")
                ?? config.UserName
                ?? "public";

            var snmpVersion = GetConnectionProperty(config.ConnectionProperties, "SnmpVersion")
                ?? GetConnectionProperty(config.ConnectionProperties, "Version")
                ?? "1";

            var versionCode = string.Equals(snmpVersion, "2", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snmpVersion, "2c", StringComparison.OrdinalIgnoreCase)
                    ? VersionCode.V2
                    : VersionCode.V1;

            var result = new DeviceConnectionTestResultDto
            {
                Success = false,
                Protocol = config.Protocol.ToString(),
                DeviceIdentifier = device.DeviceIdentifier ?? string.Empty,
                IpAddress = device.Ipaddress ?? string.Empty,
                Port = config.Port,
                OidsTried = oids
            };

            try
            {
                var endpoint = await ResolveSnmpEndpoint(device.Ipaddress, config.Port);
                var variables = oids.Select(oid => new Variable(new ObjectIdentifier(oid))).ToList();
                using var timeoutCts = new CancellationTokenSource(Math.Max(config.OperationTimeout, 1000));

                var response = await Messenger.GetAsync(
                    versionCode,
                    endpoint,
                    new OctetString(community),
                    variables,
                    timeoutCts.Token);

                foreach (var variable in response)
                {
                    result.Values[variable.Id.ToString()] = variable.Data.ToString();
                }

                result.Success = result.Values.Count > 0;
                result.Message = result.Success
                    ? $"SNMP test succeeded with {result.Values.Count} OID value(s)."
                    : "SNMP request succeeded but no OID values were returned.";

                return Ok(result);
            }
            catch (Exception ex)
            {
                result.Message = BuildSnmpFailureMessage(
                    ex,
                    result.IpAddress,
                    result.Port,
                    Math.Max(config.OperationTimeout, 1000));
                return BadRequest(result);
            }
        }

        private static string BuildSnmpFailureMessage(Exception ex, string host, int port, int timeoutMs)
        {
            if (ex is OperationCanceledException)
            {
                return $"SNMP request timed out after {timeoutMs} ms for {host}:{port}. The controller may be offline/unreachable, or SNMP settings (community/OID/port) may be incorrect.";
            }

            if (ex is SocketException socketEx)
            {
                if (socketEx.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    return $"SNMP connection to {host}:{port} was refused. Verify the controller is listening on that port and SNMP is enabled.";
                }

                if (socketEx.SocketErrorCode == SocketError.HostUnreachable
                    || socketEx.SocketErrorCode == SocketError.NetworkUnreachable
                    || socketEx.SocketErrorCode == SocketError.TimedOut)
                {
                    return $"Unable to reach {host}:{port} over SNMP. Verify controller power, network path, and firewall rules.";
                }
            }

            return $"SNMP test failed for {host}:{port}: {ex.Message}";
        }

        private static string? GetConnectionProperty(Dictionary<string, object>? connectionProperties, string key)
        {
            if (connectionProperties is null)
            {
                return null;
            }

            var value = connectionProperties
                .FirstOrDefault(kvp => string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                .Value;

            return value?.ToString();
        }

        private static async Task<IPEndPoint> ResolveSnmpEndpoint(string host, int port)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return new IPEndPoint(ipAddress, port);
            }

            var addresses = await Dns.GetHostAddressesAsync(host);
            var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses.FirstOrDefault();

            if (ipv4Address is null)
            {
                throw new InvalidOperationException($"Unable to resolve host '{host}'.");
            }

            return new IPEndPoint(ipv4Address, port);
        }

        #endregion
    }
}
