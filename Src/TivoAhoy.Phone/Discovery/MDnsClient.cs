namespace TivoAhoy.Phone.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class MDnsClient
    {
        public static readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

        private UdpAnySourceMulticastClient client;
        private IPEndPoint local;
        private ushort requestId;

        public MDnsClient(IPEndPoint endpoint)
        {
            local = endpoint;
            client = new UdpAnySourceMulticastClient(endpoint.Address, endpoint.Port);
            receiver = new Thread(StartReceiving);
        }

        public bool IsStarted { get; set; }

        public delegate void ObjectEvent<T>(T msg);
        public event ObjectEvent<Message> AnswerReceived;
        public event ObjectEvent<Message> QueryReceived;

        public bool Send(Message message, IPEndPoint ep)
        {
            try
            {
                byte[] byteMessage = message.GetBytes();
                client.BeginSendTo(byteMessage, 0, byteMessage.Length, ep, OnSendTo, null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void OnSendTo(IAsyncResult aResult)
        {
            client.EndSendTo(aResult);
        }

        public void Resolve(string protocol)
        {
            Message message = new Message();
            List<byte> guid = Guid.NewGuid().ToByteArray().Take(2).ToList();
            requestId = (ushort)(guid[0] * byte.MaxValue + guid[1]);
            message.ID = requestId;
            message.Questions.Add(new Question(protocol));
            Send(message, EndPoint);
        }

        public static MDnsClient CreateAndResolve(string protocol)
        {
            MDnsClient client = new MDnsClient(EndPoint /* new IPEndPoint(IPAddress.Any, 5353) */ );
            client.client.BeginJoinGroup(OnJoinGroup, client.client);
            try
            {
                client.Resolve(protocol);
            }
            catch (SocketException)
            {
            }

            return client;
        }
        private static void OnJoinGroup(IAsyncResult aResult)
        {
            var client = (UdpAnySourceMulticastClient)aResult.AsyncState;

            try
            {
                client.EndJoinGroup(aResult);
                client.MulticastLoopback = true;
            }
            catch (System.Net.Sockets.SocketException)
            {
            }
        }

        private AutoResetEvent active = new AutoResetEvent(false);

        private byte[] mReceived = new byte[4096];
        private void StartReceiving()
        {
            while (IsStarted)
            {
                try
                {
                    client.BeginReceiveFromGroup(mReceived, 0, mReceived.Length, StartReceiving, null);
                }
                catch (Exception)
                {
                }

                active.WaitOne();
            }
        }

        private void StartReceiving(IAsyncResult result)
        {
            //result.AsyncWaitHandle.WaitOne();
            IPEndPoint src = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                client.EndReceiveFromGroup(result, out src);
                Treat(mReceived, src);
            }
            catch (WebException)
            {
            }
        }

        protected void Treat(byte[] bytes, IPEndPoint from)
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
            ushort requestId = this.requestId;
            active.Set();
            if ((m.ID == requestId && m.QueryResponse == Qr.Answer) || m.ID == 0)
            {
                if (AnswerReceived != null)
                    AnswerReceived(m);
            }
            if ((m.ID != requestId || m.ID == 0) && m.QueryResponse == Qr.Query)
            {
                this.requestId = 0;
                if (QueryReceived != null)
                    QueryReceived(m);
            }
        }

        public void Stop()
        {
            IsStarted = false;
            active.Set();

            while (receiver.IsAlive)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
        Thread receiver;

        public void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                receiver.Start();
            }
        }
    }
}
