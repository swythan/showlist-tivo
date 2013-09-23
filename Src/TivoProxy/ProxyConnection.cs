using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto.Tls;
using Tivo.Connect;

namespace TivoProxy
{
    internal class ProxyConnection
    {
        private Stream clientStream;

        private Socket client;
        private TlsProtocolHandler protocolHandler;

        private Stream serverStream;

        private string connectionType;
        private IPAddress tivoAddress;

        public ProxyConnection(Stream clientStream)
        {
            this.clientStream = clientStream;

            Task.Factory.StartNew(this.ClientReceiveThreadProc, TaskCreationOptions.LongRunning);
        }

        public ProxyConnection(Stream clientStream, IPAddress tivoAddress)
        {
            this.tivoAddress = tivoAddress;
            this.clientStream = clientStream;

            Task.Factory.StartNew(this.ClientReceiveThreadProc, TaskCreationOptions.LongRunning);
        }

        public async Task ConnectToServer()
        {
            this.connectionType = "Away_Server";
            this.serverStream = await this.ConnectNetworkStream(new DnsEndPoint(@"secure-tivo-api.virginmedia.com", 443))
                .ConfigureAwait(false);
        }

        public async Task ConnectToTivo()
        {
            this.connectionType = "LAN_TiVo";
            this.serverStream = await this.ConnectNetworkStream(new IPEndPoint(this.tivoAddress, 1413))
                .ConfigureAwait(false);
        }

        private void ClientReceiveThreadProc()
        {
            while (true)
            {
                var receivedMessage = MindRpcFormatter.ReadMessage(this.clientStream);

                Console.WriteLine(
                    "Client -> {0} : Id={1} SchemaVersion={2}\n{3}",
                    this.connectionType,
                    MindRpcFormatter.GetRpcIdFromHeader(receivedMessage.Item1),
                    MindRpcFormatter.GetValueFromHeader("SchemaVersion", receivedMessage.Item1),
                    receivedMessage.Item2);

                var onwardMessage = MindRpcFormatter.EncodeMessage(receivedMessage.Item1, receivedMessage.Item2);

                bool firstMessage = false;
                if (this.serverStream == null)
                {
                    firstMessage = true;

                    if (this.tivoAddress != null)
                    {
                        ConnectToTivo().Wait();
                    }
                    else
                    {
                        ConnectToServer().Wait();
                    }
                }

                this.serverStream.Write(onwardMessage, 0, onwardMessage.Length);

                if (firstMessage)
                {
                    Task.Factory.StartNew(this.ServerReceiveThreadProc, TaskCreationOptions.LongRunning);
                }
            }
        }

        private void ServerReceiveThreadProc()
        {
            while (true)
            {
                var receivedMessage = MindRpcFormatter.ReadMessage(this.serverStream);

                Console.WriteLine(
                    "{0} -> Client : Id={1} SchemaVersion={2}\n{3}",
                    this.connectionType,
                    MindRpcFormatter.GetRpcIdFromHeader(receivedMessage.Item1),
                    MindRpcFormatter.GetValueFromHeader("SchemaVersion", receivedMessage.Item1),
                    receivedMessage.Item2);

                var onwardMessage = MindRpcFormatter.EncodeMessage(receivedMessage.Item1, receivedMessage.Item2);

                this.clientStream.WriteAsync(onwardMessage, 0, onwardMessage.Length);
            }
        }


        private async Task<Stream> ConnectNetworkStream(EndPoint remoteEndPoint)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            // Create a TCP/IP connection to the TiVo.
            this.client = await ConnectSocketAsync(remoteEndPoint).ConfigureAwait(false);

            Debug.WriteLine("Client connected.");

            try
            {
                // Create an SSL stream that will close the client's stream.
                var tivoTlsClient = new TivoTlsClient();

                this.protocolHandler = new TlsProtocolHandler(new NetworkStream(this.client) { ReadTimeout = Timeout.Infinite });
                this.protocolHandler.Connect(tivoTlsClient);
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

                this.protocolHandler.Close();
                this.protocolHandler = null;

                throw;
            }

            return protocolHandler.Stream;
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
