//-----------------------------------------------------------------------
// <copyright file="TivoNetworkSession.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tivo.Connect.Entities;


namespace Tivo.Connect
{
    public class TivoNetworkSession : IDisposable
    {
        private static readonly Lazy<Func<INetworkInterface>> NetworkIntefaceFactory = new Lazy<Func<INetworkInterface>>(LoadPlatformNetworkInterface);
        private readonly JsonSerializerSettings jsonSettings;
        private readonly int sessionId;

        private bool isAwayMode;
        private JsonSerializer jsonSerializer;
        private int lastRpcId = 0;
        private INetworkInterface networkInterface;
        private CancellationTokenSource receiveCancellationTokenSource;
        private Subject<Tuple<int, JObject>> receiveSubject;

        private Task receiveTask;
        private Stream sslStream = null;

        public TivoNetworkSession()
        {
            this.sessionId = new Random().Next(0x26c000, 0x27dc20);

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
            this.networkInterface = NetworkIntefaceFactory.Value();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.networkInterface != null)
                {
                    this.networkInterface.Dispose();
                    this.networkInterface = null;
                }

                if (this.receiveCancellationTokenSource != null)
                {
                    this.receiveCancellationTokenSource.Cancel();
                    // receiveCancellationTokenSource = null;
                }
            }
        }

        private static Func<INetworkInterface> LoadPlatformNetworkInterface()
        {
#if WP7
            return () => new NetworkInterface();
#else
            // get the plat lib
            try
            {
                var assemblyName = new AssemblyName(typeof(TivoNetworkSession).GetTypeInfo().Assembly.FullName)
                {
                    Name = "Tivo.Connect.Platform"
                };

                var assm = Assembly.Load(assemblyName);

                var type = assm.GetType("Tivo.Connect.Platform.NetworkInterface");

                return () => (INetworkInterface)Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new PlatformNotSupportedException(
                    "Tivo.Connect.Platform.dll was not found. " +
                    "Make sure the correct Tivo.Connect.Platform" +
                    "Platform library is referenced by your main application",
                    e);
            }
#endif
        }

        public async Task<JObject> Connect(TivoEndPoint endPoint, IDictionary<string, object> authMessage)
        {
            this.isAwayMode = endPoint.Mode == TivoMode.Away;
            this.sslStream = await this.networkInterface.Initialize(endPoint).ConfigureAwait(false);
            this.receiveSubject = new Subject<Tuple<int, JObject>>();

            // Send authentication message to the TiVo. 
            var authTask = SendRequest(authMessage);

            // Start listening on the socket *after* the first send operation.
            // This stops errors occuring on WP7
            StartReceiveThread();

            return await authTask;
        }


        private void StartReceiveThread()
        {
            this.receiveCancellationTokenSource = new CancellationTokenSource();

#if !WINDOWS_PHONE
            this.receiveTask = Task.Run(() => RpcReceiveThreadProc(), this.receiveCancellationTokenSource.Token);
#else
            this.receiveTask = TaskEx.Run(() => RpcReceiveThreadProc(), this.receiveCancellationTokenSource.Token);
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

            var bodyText = JsonConvert.SerializeObject(body, this.jsonSettings);

            var requestRpcId = Interlocked.Increment(ref this.lastRpcId);

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