using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using Org.BouncyCastle.Crypto.Tls;

namespace Tivo.Connect
{
    public partial class TivoConnection 
    {
        Socket client;
        TlsProtocolHandler protocolHandler;

        protected virtual void DisposeSpecialized(bool disposing)
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
            }
        }

        private IObservable<Stream> ConnectNetworkStream(IPAddress serverAddress)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            // Create a TCP/IP connection to the TiVo.
            return ObservableSocket.Connect(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, serverAddress, 1413)
                .ObserveOnDispatcher()
                .Select(
                    socket =>
                    {
                        Debug.WriteLine("Client connected.");

                        this.client = socket;

                        try
                        {
                            // Create an SSL stream that will close the client's stream.
                            var tivoTlsClient = new TivoTlsClient(CaptureTsnFromServerCert);

                            this.protocolHandler = new TlsProtocolHandler(new NetworkStream(socket));
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
                    });

        }

        private void CaptureTsnFromServerCert(string tsnFromCert)
        {
            string rawTsn = string.Join("", tsnFromCert.Split('-'));
            this.capturedTsn = string.Format("tsn:{0}", rawTsn);
        }
    }
}
