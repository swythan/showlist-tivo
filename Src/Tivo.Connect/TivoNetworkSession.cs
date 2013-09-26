//-----------------------------------------------------------------------
// <copyright file="TivoNetworkSession.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Tls;
using Tivo.Connect.Entities;

namespace Tivo.Connect
{
    public class TivoNetworkSession : IDisposable
    {
        private readonly int sessionId;

        private bool isAwayMode;
        private Socket client;
        private TlsProtocolHandler protocolHandler;

        private Stream sslStream = null;
        private int lastRpcId = 0;

        private Task receiveTask;
        private Subject<Tuple<int, JObject>> receiveSubject;
        private CancellationTokenSource receiveCancellationTokenSource;

        private JsonSerializerSettings jsonSettings;
        private JsonSerializer jsonSerializer;

        public TivoNetworkSession()
        {
            sessionId = new Random().Next(0x26c000, 0x27dc20);

            this.jsonSettings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatString = "yyyy'-'MM'-'dd HH':'mm':'ss",
                Converters =
                {
                    new RecordingFolderItemCreator(), 
                    new UnifiedItemCreator(),
                }
            };

            this.jsonSerializer = JsonSerializer.Create(this.jsonSettings);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.protocolHandler != null)
                {
                    this.protocolHandler.Close();
                    this.protocolHandler = null;
                }

                if (this.client != null)
                {
                    this.client.Dispose();
                    Debug.WriteLine("Client closed.");

                    this.client = null;
                }

                if (receiveCancellationTokenSource != null)
                {
                    receiveCancellationTokenSource.Cancel();
                    // receiveCancellationTokenSource = null;
                }
            }
        }

        public async Task<JObject> Connect(EndPoint endPoint, bool isAwayMode, IDictionary<string, object> authMessage)
        {
            this.isAwayMode = isAwayMode;
            this.sslStream = await ConnectNetworkStream(endPoint).ConfigureAwait(false);
            this.receiveSubject = new Subject<Tuple<int, JObject>>();

            // Send authentication message to the TiVo. 
            var authTask = SendRequest(authMessage);

            // Start listening on the socket *after* the first send operation.
            // This stops errors occuring on WP7
            StartReceiveThread();

            return await authTask;
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

        private void StartReceiveThread()
        {
            this.receiveCancellationTokenSource = new CancellationTokenSource();

#if !WINDOWS_PHONE
            receiveTask = Task.Run(() => RpcReceiveThreadProc(), receiveCancellationTokenSource.Token);
#else
            receiveTask = TaskEx.Run(() => RpcReceiveThreadProc(), receiveCancellationTokenSource.Token);
#endif
        }

        private void RpcReceiveThreadProc()
        {
            try
            {
                while (true)
                {
                    this.receiveSubject.OnNext(ReadMessage());

                    if (this.receiveCancellationTokenSource.IsCancellationRequested)
                    {
                        this.receiveSubject.OnCompleted();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                this.receiveSubject.OnError(ex);
            }
        }

        public async Task<JObject> SendRequest(IDictionary<string, object> body)
        {
            string requestType = null;

            object requestTypeObj;
            if (body.TryGetValue("type", out requestTypeObj))
            {
                requestType = requestTypeObj as string;
            }

            if (string.IsNullOrEmpty(requestType))
            {
                throw new ArgumentException("Body must contain 'type' tag", "body");
            }

            string tsn = null;
            object tsnObj;
            if (body.TryGetValue("bodyId", out tsnObj))
            {
                tsn = tsnObj as string;
            }

            string bodyText = JsonConvert.SerializeObject(body, this.jsonSettings);

            int requestRpcId = Interlocked.Increment(ref this.lastRpcId);

            var reponseObservable = this.receiveSubject
                .Where(message => message.Item1 == requestRpcId)
                .Select(message => message.Item2)
                .Take(1);

            var messageBytes = MindRpcFormatter.EncodeRequest(this.isAwayMode, this.sessionId, tsn, requestRpcId, requestType, bodyText);

            this.sslStream.Write(messageBytes, 0, messageBytes.Length);
            this.sslStream.Flush();

            return await reponseObservable;
        }

        public Tuple<int, JObject> ReadMessage()
        {
            var message = MindRpcFormatter.ReadMessage(this.sslStream);

            var body = JObject.Parse(message.Item2);

            return Tuple.Create(MindRpcFormatter.GetRpcIdFromHeader(message.Item1), body);
        }

    }
}
