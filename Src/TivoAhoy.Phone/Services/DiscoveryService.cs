using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TivoAhoy.Common.Discovery;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Settings;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using Discovery = TivoAhoy.Common.Discovery;
using System.Diagnostics;

namespace TivoAhoy.Phone.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        public async Task<IEnumerable<DiscoveredTivo>> DiscoverTivosAsync(IProgress<DiscoveredTivo> progress, CancellationToken token)
        {
            var results = new List<DiscoveredTivo>();

            var dnsAnswers = await MDnsClient.CreateAndResolveAsync(dnsProtocol);

            if (dnsAnswers != null)
            {
                var answersSubscription = dnsAnswers
                    .Select(HandleDnsAnswer)
                    .Where(x => x != null)
                    .ObserveOnDispatcher()
                    .Subscribe(
                        x => 
                        {
                            results.Add(x);

                            if (progress != null)
                            {
                                progress.Report(x);
                            }
                        });

                await TaskEx.Delay(TimeSpan.FromSeconds(3), token);

                answersSubscription.Dispose();
            }

            return results;
        }

        private DiscoveredTivo HandleDnsAnswer(Message msg)
        {
            bool isTivoResponse = false;

            foreach (var answer in msg.Answers)
            {
                if (answer.Type == Discovery.Type.PTR)
                {
                    var ptrData = (Discovery.Ptr)answer.ResponseData;
                    var name = ptrData.DomainName.ToString();

                    if (name.Contains(dnsProtocol))
                    {
                        isTivoResponse = true;
                    }
                }
            }

            if (!isTivoResponse)
            {
                return null;
            }

            int? port = null;
            IPAddress tivoAddress = null;
            string tivoName = null;
            string tivoTsn = null;

            foreach (var additional in msg.Additionals)
            {
                if (additional.Type == Discovery.Type.TXT)
                {
                    var txtData = (Discovery.Txt)additional.ResponseData;

                    foreach (var property in txtData.Properties)
                    {
                        Debug.WriteLine("TXT entry: {0} = {1}", property.Key, property.Value);
                        if (property.Key == "TSN")
                        {
                            tivoTsn = property.Value;
                        }
                    }
                }

                if (additional.Type == Discovery.Type.A)
                {
                    var hostAddress = (Discovery.HostAddress)additional.ResponseData;

                    tivoAddress = hostAddress.Address;
                    tivoName = additional.DomainName[0];
                }

                if (additional.Type == Discovery.Type.SRV)
                {
                    var srvData = (Discovery.Srv)additional.ResponseData;

                    port = srvData.Port;
                }
            }

            Debug.WriteLine("TiVo found at {0}:{1}", msg.From.Address, port);

            return new DiscoveredTivo(tivoName, tivoAddress, tivoTsn);
        }
    }
}
