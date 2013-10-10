using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;

namespace Tivo.Connect.Platform
{
    public class NetworkInterface : INetworkInterface
    {
        private TcpClient client;
        private TlsProtocolHandler tlsProtocolHandler;

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot call Initialize on an already connected interface");
            }

            // Create new connection
            this.client = new TcpClient();

            await this.client.ConnectAsync(endPoint.Address, endPoint.Port).ConfigureAwait(false);

            var tivoTlsClient = new TivoTlsClient(endPoint.Certificate, endPoint.Password);

            this.tlsProtocolHandler = new TlsProtocolHandler(this.client.GetStream());
            this.tlsProtocolHandler.Connect(tivoTlsClient);

            return this.tlsProtocolHandler.Stream;
        }

        public void Dispose()
        {
            try
            {
                if (this.client != null)
                {
                    this.client.Close();
                    this.client = null;
                }

                if (this.tlsProtocolHandler != null)
                {
                    this.tlsProtocolHandler.Close();
                    this.tlsProtocolHandler = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}