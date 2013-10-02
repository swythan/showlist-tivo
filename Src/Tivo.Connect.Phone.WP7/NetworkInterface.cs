using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;

// ReSharper disable once CheckNamespace
namespace Tivo.Connect
{
    internal class NetworkInterface : INetworkInterface
    {
        private Socket client;
        private TlsProtocolHandler tlsProtocolHandler;

        public async Task<Stream> Initialize(TivoEndPoint endPoint)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            var ep = new DnsEndPoint(endPoint.Address, (int)endPoint.Mode);

            // Create a TCP/IP connection to the TiVo.
            this.client = await ConnectSocketAsync(ep).ConfigureAwait(false);

            ////Debug.WriteLine("Client connected.");

            try
            {
                // Create an SSL stream that will close the client's stream.
                var tivoTlsClient = new TivoTlsClient(endPoint.Certificate, endPoint.Password);

                this.tlsProtocolHandler = new TlsProtocolHandler(new NetworkStream(this.client) {ReadTimeout = Timeout.Infinite});
                this.tlsProtocolHandler.Connect(tivoTlsClient);
            }
            catch (IOException e)
            {
                Debug.WriteLine("Authentication failed - closing the connection.");

                Debug.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Debug.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }

                this.client.Dispose();
                this.client = null;

                this.tlsProtocolHandler.Close();
                this.tlsProtocolHandler = null;

                throw;
            }

            return this.tlsProtocolHandler.Stream;
        }


        public void Dispose()
        {
            if (this.client != null)
            {
                this.client.Dispose();
                this.client = null;
            }

            if (this.tlsProtocolHandler != null)
            {
                this.tlsProtocolHandler.Close();
                this.tlsProtocolHandler = null;
            }
        }

        private static async Task<Socket> ConnectSocketAsync(EndPoint remoteEndPoint)
        {
            var tcs = new TaskCompletionSource<Socket>();

            var args = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = remoteEndPoint,
            };

            args.Completed +=
                (sender, e) =>
                {
                    if (e.ConnectByNameError != null)
                    {
                        tcs.TrySetException(e.ConnectByNameError);
                    }
                    else
                    {
                        if (e.SocketError != SocketError.Success)
                        {
                            tcs.TrySetException(new SocketException((int)e.SocketError));
                        }
                        else
                        {
                            tcs.TrySetResult(e.ConnectSocket);
                        }
                    }
                };

            if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args))
            {
                return args.ConnectSocket;
            }

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}