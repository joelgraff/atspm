#region license
// Copyright 2026 Utah Department of Transportation
// for Infrastructure - Utah.Udot.Atspm.Infrastructure.Services.DownloaderClients/SharpSnmpDownloaderClient.cs
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

using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using System.Net;
using Utah.Udot.Atspm.Data.Enums;

namespace Utah.Udot.Atspm.Infrastructure.Services.DownloaderClients
{
    /// <summary>
    /// SNMP client for NTCIP access using SharpSnmpLib.
    /// </summary>
    public class SharpSnmpDownloaderClient : DownloaderClientBase
    {
        private IPEndPoint _receiver;
        private OctetString _community;
        private VersionCode _version;
        private int _operationTimeoutMs;
        private bool _connected;

        /// <inheritdoc/>
        public override TransportProtocols Protocol => TransportProtocols.Snmp;

        /// <inheritdoc/>
        public override bool IsConnected => _connected && _receiver != null && _community != null;

        /// <inheritdoc/>
        protected override Task Connect(IPEndPoint connection, NetworkCredential credentials, int connectionTimeout = 2000, int operationTimeout = 2000, Dictionary<string, string> connectionProperties = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            var community = credentials?.UserName;
            if (string.IsNullOrWhiteSpace(community) && connectionProperties != null && connectionProperties.TryGetValue("Community", out var configuredCommunity))
            {
                community = configuredCommunity;
            }

            if (string.IsNullOrWhiteSpace(community))
            {
                community = "public";
            }

            _version = VersionCode.V1;
            if (connectionProperties != null && connectionProperties.TryGetValue("Version", out var configuredVersion))
            {
                var normalized = configuredVersion?.Trim().ToLowerInvariant();
                if (normalized == "2" || normalized == "2c" || normalized == "v2" || normalized == "v2c")
                {
                    _version = VersionCode.V2;
                }
            }

            _receiver = connection;
            _community = new OctetString(community);
            _operationTimeoutMs = Math.Max(100, operationTimeout);
            _connected = true;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task DeleteResource(Uri resource, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task Disconnect(CancellationToken token = default)
        {
            _connected = false;
            _receiver = null;
            _community = null;

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task<FileInfo> DownloadResource(FileInfo file, Uri remote, CancellationToken token = default)
        {
            var oidText = remote.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(oidText))
            {
                throw new ArgumentException("SNMP OID is missing from resource URI path.", nameof(remote));
            }

            var variables = new List<Variable> { new(new ObjectIdentifier(oidText)) };

            using var timeoutCts = new CancellationTokenSource(_operationTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);

            var response = await Messenger.GetAsync(_version, _receiver, _community, variables, linkedCts.Token);
            var result = response.FirstOrDefault();

            if (result == null)
            {
                throw new InvalidOperationException($"No SNMP response value for OID '{oidText}'.");
            }

            var content = $"{result.Id} = {result.Data}";
            await File.WriteAllTextAsync(file.FullName, content, token).ConfigureAwait(false);

            return file;
        }

        /// <inheritdoc/>
        protected override Task<IEnumerable<Uri>> ListResources(string path, CancellationToken token = default, params string[] query)
        {
            var oids = query?.Where(q => !string.IsNullOrWhiteSpace(q)).Select(q => q.Trim()).ToArray() ?? [];

            if (oids.Length == 0)
            {
                oids = ["1.3.6.1.2.1.1.1.0"];
            }

            var resources = oids
                .Select(oid => new Uri($"snmp://{_receiver.Address}:{_receiver.Port}/{oid}"))
                .ToArray();

            return Task.FromResult<IEnumerable<Uri>>(resources);
        }
    }
}