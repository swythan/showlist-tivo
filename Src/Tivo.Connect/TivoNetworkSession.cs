//-----------------------------------------------------------------------
// <copyright file="TivoNetworkSession.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly int _sessionId;

        private bool _isAwayMode;
        private INetworkInterface _networkInterface;

        private Stream _sslStream = null;
        private int _lastRpcId = 0;

        private Task receiveTask;
        private Subject<Tuple<int, JObject>> _receiveSubject;
        private CancellationTokenSource _receiveCancellationTokenSource;

        private readonly JsonSerializerSettings jsonSettings;
        private JsonSerializer _jsonSerializer;

        private static readonly Lazy<Func<INetworkInterface>> NetworkIntefaceFactory = new Lazy<Func<INetworkInterface>>(LoadPlatformNetworkInterface);

        public TivoNetworkSession()
        {
            _sessionId = new Random().Next(0x26c000, 0x27dc20);

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

            this._jsonSerializer = JsonSerializer.Create(this.jsonSettings);
            _networkInterface = NetworkIntefaceFactory.Value();
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
                if (_networkInterface != null)
                {
                    _networkInterface.Dispose();
                    _networkInterface = null;
                }

                if (_receiveCancellationTokenSource != null)
                {
                    _receiveCancellationTokenSource.Cancel();
                    // receiveCancellationTokenSource = null;
                }
            }
        }

        private static Func<INetworkInterface> LoadPlatformNetworkInterface()
        {
#if WP7
            return () => new Tivo.Connect.Platform.NetworkInterface();
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
            _isAwayMode = endPoint.Mode == TivoMode.Away;
            _sslStream = await _networkInterface.Initialize(endPoint).ConfigureAwait(false);
            _receiveSubject = new Subject<Tuple<int, JObject>>();

            // Send authentication message to the TiVo. 
            var authTask = SendRequest(authMessage);

            // Start listening on the socket *after* the first send operation.
            // This stops errors occuring on WP7
            StartReceiveThread();

            return await authTask;
        }


        private void StartReceiveThread()
        {
            this._receiveCancellationTokenSource = new CancellationTokenSource();

#if !WINDOWS_PHONE
            receiveTask = Task.Run(() => RpcReceiveThreadProc(), _receiveCancellationTokenSource.Token);
#else
            receiveTask = TaskEx.Run(() => RpcReceiveThreadProc(), _receiveCancellationTokenSource.Token);
#endif
        }

        private void RpcReceiveThreadProc()
        {
            try
            {
                while (true)
                {
                    this._receiveSubject.OnNext(ReadMessage());

                    if (this._receiveCancellationTokenSource.IsCancellationRequested)
                    {
                        this._receiveSubject.OnCompleted();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                this._receiveSubject.OnError(ex);
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

            int requestRpcId = Interlocked.Increment(ref this._lastRpcId);

            var reponseObservable = this._receiveSubject
                .Where(message => message.Item1 == requestRpcId)
                .Select(message => message.Item2)
                .Take(1);

            var messageBytes = MindRpcFormatter.EncodeRequest(this._isAwayMode, this._sessionId, tsn, requestRpcId, requestType, bodyText);

            this._sslStream.Write(messageBytes, 0, messageBytes.Length);
            this._sslStream.Flush();

            return await reponseObservable;
        }

        public Tuple<int, JObject> ReadMessage()
        {
            var message = MindRpcFormatter.ReadMessage(this._sslStream);

            var body = JObject.Parse(message.Item2);

            return Tuple.Create(MindRpcFormatter.GetRpcIdFromHeader(message.Item1), body);
        }

    }
}
