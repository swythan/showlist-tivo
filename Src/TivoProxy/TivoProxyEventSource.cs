using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tivo.Connect;

namespace TivoProxy
{
    [EventSource(Name = "TivoProxy")]
    public class TivoProxyEventSource : EventSource
    {
        private static readonly Lazy<TivoProxyEventSource> Instance =
            new Lazy<TivoProxyEventSource>(() => new TivoProxyEventSource());

        private TivoProxyEventSource()
        {
        }

        public static TivoProxyEventSource Log
        {
            get { return Instance.Value; }
        }

        [Event(1, Message = "Client connected [{0}] : Client = {1}")]
        public void ClientConnected(TivoConnectionMode mode, string clientAddress, string streamProperties)
        {
            WriteEvent(1, mode, clientAddress, streamProperties);
        }

        [NonEvent]
        public void ClientConnectionFailure(TivoConnectionMode mode, Exception exception)
        {
            this.ClientConnectionFailure(mode, exception.ToString());
        }

        [Event(2, Message = "Failure authenticating client connection [{0}] :\n{1}")]
        public void ClientConnectionFailure(TivoConnectionMode mode, string exceptionText)
        {
            WriteEvent(2, mode, exceptionText);
        }

        [Event(3, Message = "Server connected [{0}] : Server = {1}")]
        public void ServerConnected(TivoConnectionMode mode, string serverAddress)
        {
            WriteEvent(3, mode, serverAddress);
        }

        [NonEvent]
        public void ServerConnectionFailure(TivoConnectionMode mode, Exception exception)
        {
            this.ServerConnectionFailure(mode, exception.ToString());
        }

        [Event(4, Message = "Failure authenticating server connection [{0}] :\n{1}")]
        public void ServerConnectionFailure(TivoConnectionMode mode, string exceptionText)
        {
            WriteEvent(4, mode, exceptionText);
        }

        [Event(5, Message = "SENT -> {0} v{1}: {2} - {3}\n{4}\n")]
        public void MessageFromClient(TivoConnectionMode mode, int schemaVersion, int rpcId, string type, string message)
        {
            WriteEvent(5, mode, schemaVersion, rpcId, type, message);
        }

        [Event(6, Message = "{0} -> RECV v{1}: {2} - {3}\n{4}\n")]
        public void MessageFromServer(TivoConnectionMode mode, int schemaVersion, int rpcId, string type, string message)
        {
            WriteEvent(6, mode, schemaVersion, rpcId, type, message);
        }
    }
}
