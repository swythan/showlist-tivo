using System;
using System.Collections.Generic;
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
        const string tivoService = "_tivo-mindrpc._tcp.local.";

        public async Task<IEnumerable<DiscoveredTivo>> DiscoverTivosAsync(IProgress<DiscoveredTivo> progress, CancellationToken token)
        {
            Action<IZeroconfHost> callback = null;

            if (progress != null)
            {
                callback = x => progress.Report(GetDiscoveredTivoFromHost(x));
            }

            var mDnsResults = await ZeroconfResolver.ResolveAsync(tivoService, 
                TimeSpan.FromSeconds(.5),
                callback: callback,
                cancellationToken: token)
                .ConfigureAwait(false);

            return mDnsResults.Select(GetDiscoveredTivoFromHost).ToList();
        }

        private static DiscoveredTivo GetDiscoveredTivoFromHost(IZeroconfHost host)
        {
            return new DiscoveredTivo(host.DisplayName, IPAddress.Parse(host.IPAddress), host.Services.First().Value.Properties[0]["TSN"]);
        }
    }
}
