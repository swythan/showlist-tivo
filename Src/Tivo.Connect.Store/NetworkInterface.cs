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
        private StreamSocket _streamSocket;

        private TlsProtocolHandler _tlsProtocolHandler;


        public void Dispose()
        {
            _streamSocket.Dispose();
            _streamSocket = null;
        }

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {
            if (_streamSocket != null)
                throw new InvalidOperationException("Cannot call Initialize on an open interface");


            _streamSocket = new StreamSocket();


            await _streamSocket.ConnectAsync(new HostName(endPoint.Address),
                                             ((int)endPoint.Mode).ToString(CultureInfo.InvariantCulture),
                                             SocketProtectionLevel.PlainSocket).AsTask().ConfigureAwait(false);

            
            var readStream = _streamSocket.InputStream.AsStreamForRead();
            var writeStream = _streamSocket.OutputStream.AsStreamForWrite();


            var tivoTlsClient = new TivoTlsClient(endPoint.Certificate, endPoint.Password);
            _tlsProtocolHandler = new TlsProtocolHandler(readStream, writeStream);
            _tlsProtocolHandler.Connect(tivoTlsClient);

            return _tlsProtocolHandler.Stream;
        }
    }
}