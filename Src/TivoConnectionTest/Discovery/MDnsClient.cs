namespace TivoAhoy.Common.Discovery
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

        private UdpClient client;
        private IPEndPoint local;

        private Task receiverTask;
        private CancellationTokenSource receiverCts;

        private ReplaySubject<Message> answers = new ReplaySubject<Message>();

        public MDnsClient(IPEndPoint endpoint)
        {
            local = endpoint;
            client = new UdpClient(AddressFamily.InterNetwork);
        }

        public void Dispose()
        {
            this.Stop();

            if (this.client != null)
            {
                this.client.Close();
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

            await client.SendAsync(byteMessage, byteMessage.Length, EndPoint);

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
                client.Start();

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
                    var receiveResult = await client.ReceiveAsync();

                    DecodeMessage(receiveResult.Buffer, receiveResult.RemoteEndPoint);
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

        public void Start()
        {
            if (!IsStarted)
            {
                //this.client.Client.Bind(new IPEndPoint(IPAddress.Any, 12000));

                this.client.JoinMulticastGroup(EndPoint.Address);

                this.client.MulticastLoopback = true;

                this.receiverCts = new CancellationTokenSource();
                this.receiverTask = Task.Factory.StartNew(StartReceiving, this.receiverCts.Token);
            }
        }
    }
}
