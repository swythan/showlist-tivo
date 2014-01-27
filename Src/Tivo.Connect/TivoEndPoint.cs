using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tivo.Connect
{
    public class TivoEndPoint : IMindRpcHeaderInfo
    {
        private readonly TivoServiceProvider serviceProvider;
        private readonly ICertificateStore certificateStore;
        private readonly string rpcAppName;
        private readonly Version rpcAppVersion;

        public static TivoEndPoint CreateAway(TivoServiceProvider serviceProvider, ICertificateStore certificateStore)
        {
            return new TivoEndPoint(GetMiddlemindServerAddress(serviceProvider), TivoConnectionMode.Away, serviceProvider, certificateStore);
        }

        private static string GetMiddlemindServerAddress(TivoServiceProvider serviceProvider)
        {
            string address;
            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    address = "middlemind.tivo.com";
                    break;

                case TivoServiceProvider.VirginMediaUK:
                    address = "secure-tivo-api.virginmedia.com";
                    break;

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("serviceProvider", "Must specify a valid service provider.");
            }

            return address;
        }

        public static TivoEndPoint CreateLocal(string address, TivoServiceProvider serviceProvider, ICertificateStore certificateStore)
        {
            return new TivoEndPoint(address, TivoConnectionMode.Local, serviceProvider, certificateStore);
        }

        private TivoEndPoint(string address, TivoConnectionMode mode, TivoServiceProvider serviceProvider, ICertificateStore certificateStore)
        {
            if (certificateStore == null)
                throw new ArgumentNullException("certificateStore");

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("address");

            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    this.rpcAppName = "Quicksilver";
                    this.rpcAppVersion = new Version(1, 2);
                    break;
                case TivoServiceProvider.VirginMediaUK:
                    this.rpcAppName = "com.virginmedia.quicksilvervm";
                    this.rpcAppVersion = new Version(2, 2);
                    break;
                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("service", "Must specify a valid service provider.");
            }

            this.serviceProvider = serviceProvider;
            this.certificateStore = certificateStore;
            this.Address = address;
            this.ConnectionMode = mode;
        }

        public TivoConnectionMode ConnectionMode { get; private set; }
        public string Address { get; private set; }

        public int Port 
        {
            get
            {
                switch (this.ConnectionMode)
                {
                    case TivoConnectionMode.Away:
                        return 443;
                    case TivoConnectionMode.Local:
                        return 1413;
                    default:
                        throw new InvalidOperationException("Unsupported ConnectionMode");
                }
            }
        }

        public Stream Certificate
        {
            get
            {
                return this.certificateStore.GetCertificate(this.serviceProvider, false);
            }
        }

        public string Password
        {
            get
            {
                return this.certificateStore.GetPassword(this.serviceProvider, false);
            }
        }

        string IMindRpcHeaderInfo.ApplicationName
        {
            get { return this.rpcAppName; }
        }

        Version IMindRpcHeaderInfo.ApplicationVersion
        {
            get { return this.rpcAppVersion; }
        }
    }
}
