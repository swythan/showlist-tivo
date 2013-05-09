namespace TivoAhoy.Phone.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    public class MDnsClient : IDisposable
    {
        public static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

        private UdpAnySourceMulticastClient client;
        private IPEndPoint local;

        private Task receiverTask;
        private CancellationTokenSource receiverCts;

        private Subject<Message> answers = new Subject<Message>();

        public MDnsClient(IPEndPoint endpoint)
        {
            local = endpoint;
            client = new UdpAnySourceMulticastClient(endpoint.Address, endpoint.Port);
        }

        public void Dispose()
        {
            this.Stop();

            if (this.client != null)
            {
                this.client.Dispose();
            }
        }

        public bool IsStarted
        {
            get { return this.receiverTask != null; }
        }

        public async Task<IObservable<Message>> ResolveAsync(string protocol)
        {
            ushort requestId = CreateRequestId();

            Message message = new Message(requestId);
            message.Questions.Add(new Question(protocol));

            byte[] byteMessage = message.GetBytes();

            await Task.Factory.FromAsync(
                (buffer, ep, callback, state) => client.BeginSendTo(buffer, 0, buffer.Length, ep, callback, state),
                client.EndSendTo,
                byteMessage,
                EndPoint,
                null);

            return this.answers.Where(x => x.ID == requestId || x.ID == 0);
        }

        private static ushort CreateRequestId()
        {
            List<byte> guid = Guid.NewGuid()
                .ToByteArray()
                .Take(2)
                .ToList();

            return (ushort)(guid[0] * byte.MaxValue + guid[1]);
        }

        public async static Task<IObservable<Message>> CreateAndResolveAsync(string protocol)
        {
            MDnsClient client = new MDnsClient(EndPoint /* new IPEndPoint(IPAddress.Any, 5353) */ );

            try
            {
                await client.StartAsync();

                var answers = await client.ResolveAsync(protocol);

                return Observable.Using(
                    () => client,
                    _ => answers);
            }
            catch (SocketException)
            {
                return Observable.Empty<Message>();
            }
        }

        private async void StartReceiving()
        {
            CancellationToken token = this.receiverCts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] receivedData = new byte[4096];
                    var receiveResult = await Task.Factory.FromAsync(
                        client.BeginReceiveFromGroup,
                        (iar) =>
                        {
                            IPEndPoint endpoint;
                            int result = client.EndReceiveFromGroup(iar, out endpoint);

                            return Tuple.Create(result, endpoint);
                        },
                        receivedData,
                        0,
                        receivedData.Length, null, TaskCreationOptions.None);

                    DecodeMessage(receivedData, receiveResult.Item2);
                }
                catch (Exception)
                {
                }
            }
        }

        protected void DecodeMessage(byte[] bytes, IPEndPoint from)
        {
            Message m;
            try
            {
                using (var r = new System.IO.BinaryReader(new System.IO.MemoryStream(bytes)))
                {
                    m = Message.GetMessage(r);
                }
            }
            catch (Exception)
            {
                return;
            }

            m.From = from;

            if (m.QueryResponse == Qr.Answer)
            {
                this.answers.OnNext(m);
            }

            //if (m.QueryResponse == Qr.Query)
            //{
            //    if (QueryReceived != null)
            //        QueryReceived(this, new MessageEventArgs(m));
            //}
        }

        public void Stop()
        {
            if (this.receiverCts != null)
            {
                this.receiverCts.Cancel();
            }

            if (this.receiverTask != null)
            {
                this.receiverTask.Wait();

                this.receiverCts = null;
                this.receiverTask = null;
            }
        }

        public async Task StartAsync()
        {
            if (!IsStarted)
            {
                await Task.Factory.FromAsync(
                    this.client.BeginJoinGroup,
                    this.client.EndJoinGroup,
                    null);

                this.client.MulticastLoopback = true;

                this.receiverCts = new CancellationTokenSource();
                this.receiverTask = Task.Factory.StartNew(StartReceiving, this.receiverCts.Token);
            }
        }
    }
}
