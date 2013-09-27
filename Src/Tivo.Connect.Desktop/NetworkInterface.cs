using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Tivo.Connect.Platform
{
    public class NetworkInterface : INetworkInterface
    {
        private TcpClient client;
        private SslStream stream;

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {
            if (this.client != null && this.client.Connected)
            {
                throw new InvalidOperationException("Cannot call Initialize on an already connected interface");
            }

            if (this.client != null && !this.client.Connected)
            {
                this.stream.Dispose();
                this.client.Close();
            }

            byte[] certBytes;
            using (var ms = new MemoryStream())
            {
                await endPoint.Certificate.CopyToAsync(ms).ConfigureAwait(false);
                certBytes = ms.ToArray();
            }

            var cert = new X509Certificate2(certBytes, endPoint.Password);

            // Create new connection
            this.client = new TcpClient();

            await this.client.ConnectAsync(endPoint.Address, (int)endPoint.Mode).ConfigureAwait(false);

            this.stream = new SslStream(
                this.client.GetStream(),
                true,
                // close inner stream on dispose
                (sender, certificate, chain, errors) => true,
                // ignore cert errors
                (sender, host, certificates, certificate, issuers) => cert); // provide local cert

            this.stream.WriteTimeout = 10000;

            await this.stream.AuthenticateAsClientAsync("Unused", new X509Certificate2Collection(), SslProtocols.Tls, false).ConfigureAwait(false);

            return this.stream;
        }

        public void Dispose()
        {
            try
            {
                if (this.client.Connected)
                {
                    this.client.Close();
                    this.stream.Dispose();

                    this.client = null;
                    this.stream = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}