#region license
// Copyright 2026 Utah Department of Transportation
// for Infrastructure - Utah.Udot.Atspm.Infrastructure.Services.EventLogDecoders/SnmpToIndianaDecoder.cs
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

using System.Text.RegularExpressions;
using Utah.Udot.Atspm.Data.Models.EventLogModels;

namespace Utah.Udot.Atspm.Infrastructure.Services.EventLogDecoders
{
    /// <summary>
    /// Converts SNMP OID/value text lines into synthetic Indiana events for pipeline compatibility.
    /// Expected line format: &lt;oid&gt; = &lt;value&gt;
    /// </summary>
    public class SnmpToIndianaDecoder : EventLogDecoderBase<IndianaEvent>
    {
        private static readonly Regex FirstIntegerRegex = new(@"-?\d+", RegexOptions.Compiled);

        /// <inheritdoc/>
        public override IEnumerable<IndianaEvent> Decode(Device device, Stream stream, CancellationToken cancelToken = default)
        {
            cancelToken.ThrowIfCancellationRequested();

            ArgumentNullException.ThrowIfNull(device);
            if (stream?.Length == 0)
                throw new InvalidDataException("Stream is empty");

            var locationIdentifier = device.Location.LocationIdentifier;
            var decoded = new List<IndianaEvent>();

            stream.Position = 0;

            using var reader = new StreamReader(stream, leaveOpen: true);
            string line;
            var index = 0;
            var baseTimestamp = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            while ((line = reader.ReadLine()) != null)
            {
                cancelToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                var oid = parts.Length > 0 ? parts[0] : string.Empty;
                var valueText = parts.Length > 1 ? parts[1] : string.Empty;

                var eventCode = MapOidToEventCode(oid);
                var eventParam = MapValueToEventParam(valueText);

                decoded.Add(new IndianaEvent
                {
                    LocationIdentifier = locationIdentifier,
                    Timestamp = baseTimestamp.AddMilliseconds(index),
                    EventCode = eventCode,
                    EventParam = eventParam
                });

                index++;
            }

            return decoded;
        }

        private static short MapOidToEventCode(string oid)
        {
            if (!string.IsNullOrWhiteSpace(oid))
            {
                var tail = oid.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (short.TryParse(tail, out var parsedTail) && parsedTail >= 0)
                    return parsedTail;
            }

            var hash = Math.Abs((oid ?? string.Empty).GetHashCode());
            return (short)(1000 + (hash % 30000));
        }

        private static short MapValueToEventParam(string valueText)
        {
            if (string.IsNullOrWhiteSpace(valueText))
                return 0;

            var match = FirstIntegerRegex.Match(valueText);
            if (match.Success && int.TryParse(match.Value, out var parsed))
            {
                if (parsed > short.MaxValue)
                    return short.MaxValue;
                if (parsed < short.MinValue)
                    return short.MinValue;
                return (short)parsed;
            }

            return 0;
        }
    }
}