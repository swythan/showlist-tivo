using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TivoAhoy.Common.Discovery;

namespace TivoAhoy.Common.Services
{
    public interface IDiscoveryService
    {
        Task<IEnumerable<DiscoveredTivo>> DiscoverTivosAsync(IProgress<DiscoveredTivo> progress, CancellationToken ct);
    }
}
