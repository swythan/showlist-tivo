using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Org.BouncyCastle.Crypto.Tls;

namespace Tivo.Connect.Platform
{
    internal class NetworkInterface : INetworkInterface
    {
        private StreamSocket streamSocket;

        private TlsProtocolHandler tlsProtocolHandler;

        public void Dispose()
        {
            if (this.tlsProtocolHandler != null)
            {
                this.tlsProtocolHandler.Close();
                this.tlsProtocolHandler = null;
            }

            if (this.streamSocket != null)
            {
                this.streamSocket.Dispose();
                this.streamSocket = null;
            }
        }

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {
            if (this.streamSocket != null)
                throw new InvalidOperationException("Cannot call Initialize on an open interface");
            
            this.streamSocket = new StreamSocket();

            await this.streamSocket.ConnectAsync(new HostName(endPoint.Address),
                                                 endPoint.Port.ToString(CultureInfo.InvariantCulture),
                                                 SocketProtectionLevel.PlainSocket).AsTask().ConfigureAwait(false);

            var readStream = this.streamSocket.InputStream.AsStreamForRead();
            var writeStream = this.streamSocket.OutputStream.AsStreamForWrite();

            var tivoTlsClient = new TivoTlsClient(endPoint.Certificate, endPoint.Password);
            this.tlsProtocolHandler = new TlsProtocolHandler(readStream, writeStream);
            this.tlsProtocolHandler.Connect(tivoTlsClient);

            return this.tlsProtocolHandler.Stream;
        }
    }
}