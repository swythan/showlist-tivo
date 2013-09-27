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
        private TcpClient _client;
        private SslStream _stream;

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {

            if (_client != null && _client.Connected)
                throw new InvalidOperationException("Cannot call Initialize on an already connected interface");

            if (_client != null && !_client.Connected)
            {
                _stream.Dispose();
                _client.Close();
            }

            byte[] certBytes;
            using (var ms = new MemoryStream())
            {
                await endPoint.Certificate.CopyToAsync(ms).ConfigureAwait(false);
                certBytes = ms.ToArray();
            }
            var cert = new X509Certificate2(certBytes, endPoint.Password);

            // Create new connection
            _client = new TcpClient();
            
            await _client.ConnectAsync(endPoint.Address, (int)endPoint.Mode).ConfigureAwait(false);

            _stream = new SslStream(_client.GetStream(),
                                    true, // close inner stream on dispose
                                    (sender, certificate, chain, errors) => true, // ignore cert errors
                                    (sender, host, certificates, certificate, issuers) => cert); // provide local cert

            _stream.WriteTimeout = 10000;
            
            await _stream.AuthenticateAsClientAsync("Unused", new X509Certificate2Collection(), SslProtocols.Tls, false).ConfigureAwait(false);

            return _stream;
        }



        public void Dispose()
        {
            try
            {
                if (_client.Connected)
                {
                    _client.Close();
                    _stream.Dispose();

                    _client = null;
                    _stream = null;
                }
            }
            catch (Exception)
            {


            }
        }
    }
}
