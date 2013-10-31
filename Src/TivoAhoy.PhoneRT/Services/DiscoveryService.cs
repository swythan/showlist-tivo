using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TivoAhoy.Common.Discovery;
using TivoAhoy.Common.Services;
using Zeroconf;

namespace TivoAhoy.PhoneRT.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private const string tivoService = "_tivo-mindrpc._tcp.local.";

        public async Task<IEnumerable<DiscoveredTivo>> DiscoverTivosAsync(IProgress<DiscoveredTivo> progress, CancellationToken ct)
        {
            Action<IZeroconfHost> callback = null;

            if (progress != null)
            {
                callback = x =>
                    {
                        var tivo = CreateDiscoveredTivoFromResponse(x);

                        if (tivo != null)
                        {
                            progress.Report(tivo);
                        }
                    };
            }

            var zeroConfResults = await ZeroconfResolver.ResolveAsync(
                tivoService,
                TimeSpan.FromSeconds(1),
                2,
                2000,
                callback,
                ct);

            return zeroConfResults.Select(CreateDiscoveredTivoFromResponse)
                .Where(x => x != null)
                .ToList();
        }

        private static DiscoveredTivo CreateDiscoveredTivoFromResponse(IZeroconfHost host)
        {
            var matchingServices = host.Services
                .Where(x => string.Equals(x.Key, tivoService, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value);

            var tivoDetails = matchingServices
                .SelectMany(
                    x => x.Properties.Select(
                        d =>
                        {
                            string tsn;
                            string platform;
                            if (d.TryGetValue("TSN", out tsn) &&
                                d.TryGetValue("platform", out platform))
                            {
                                return new { TSN = tsn, Platform = platform };
                            }

                            return null;
                        }))
                .FirstOrDefault(y => y != null);

            return new DiscoveredTivo(
                host.DisplayName,
                IPAddress.Parse(host.IPAddress),
                tivoDetails.TSN,
                tivoDetails.Platform);
        }
    }
}
