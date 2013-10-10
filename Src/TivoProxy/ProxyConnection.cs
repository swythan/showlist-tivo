//-----------------------------------------------------------------------
// <copyright file="ProxyConnection.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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

        private TivoEndPoint serverEndPoint;

        public ProxyConnection(Stream clientStream, TivoEndPoint serverEndPoint)
        {
            this.serverEndPoint = serverEndPoint;
            this.clientStream = clientStream;

            Task.Factory.StartNew(this.ClientReceiveThreadProc, TaskCreationOptions.LongRunning);
        }

        public async Task ConnectToServer()
        {
            this.serverStream = await this.ConnectNetworkStream()
                .ConfigureAwait(false);
        }

        private void ClientReceiveThreadProc()
        {
            while (true)
            {
                var receivedMessage = MindRpcFormatter.ReadMessage(this.clientStream);

                TivoProxyEventSource.Log.MessageFromClient(
                    this.serverEndPoint.ConnectionMode,
                    MindRpcFormatter.GetSchemaVersionFromHeader(receivedMessage.Item1),
                    MindRpcFormatter.GetRpcIdFromHeader(receivedMessage.Item1),
                    MindRpcFormatter.GetTypeFromHeader(receivedMessage.Item1),
                    receivedMessage.Item2);

                var onwardMessage = MindRpcFormatter.EncodeMessage(receivedMessage.Item1, receivedMessage.Item2);

                bool firstMessage = false;
                if (this.serverStream == null)
                {
                    firstMessage = true;

                    ConnectToServer().Wait();
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

                TivoProxyEventSource.Log.MessageFromServer(
                    this.serverEndPoint.ConnectionMode, 
                    MindRpcFormatter.GetSchemaVersionFromHeader(receivedMessage.Item1), 
                    MindRpcFormatter.GetRpcIdFromHeader(receivedMessage.Item1), 
                    MindRpcFormatter.GetTypeFromHeader(receivedMessage.Item1), 
                    receivedMessage.Item2);

                var onwardMessage = MindRpcFormatter.EncodeMessage(receivedMessage.Item1, receivedMessage.Item2);

                this.clientStream.WriteAsync(onwardMessage, 0, onwardMessage.Length);
            }
        }

        private async Task<Stream> ConnectNetworkStream()
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            // Create a TCP/IP connection to the TiVo.
            if (this.serverEndPoint.ConnectionMode == TivoConnectionMode.Away)
            {
                this.client = await ConnectSocketAsync(new DnsEndPoint(this.serverEndPoint.Address, this.serverEndPoint.Port))
                    .ConfigureAwait(false);
            }
            else
            {
                this.client = await ConnectSocketAsync(new IPEndPoint(IPAddress.Parse(this.serverEndPoint.Address), this.serverEndPoint.Port))
                    .ConfigureAwait(false);
            }

            TivoProxyEventSource.Log.ServerConnected(this.serverEndPoint.ConnectionMode, this.serverEndPoint.Address);

            try
            {
                // Create an SSL stream that will close the client's stream.
                var tivoTlsClient = new TivoTlsClient(this.serverEndPoint.Certificate, this.serverEndPoint.Password);

                this.protocolHandler = new TlsProtocolHandler(new NetworkStream(this.client) { ReadTimeout = Timeout.Infinite });
                this.protocolHandler.Connect(tivoTlsClient);

                TivoProxyEventSource.Log.ServerConnected(this.serverEndPoint.ConnectionMode, this.serverEndPoint.Address);
            }
            catch (IOException e)
            {
                TivoProxyEventSource.Log.ServerConnectionFailure(this.serverEndPoint.ConnectionMode, e);

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
