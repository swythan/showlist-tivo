using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using Org.BouncyCastle.Crypto.Tls;
using System.Threading;

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

        private Stream ConnectNetworkStream(IPAddress serverAddress)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }


            // Create a TCP/IP connection to the TiVo.
            try
            {
                var endpoint = new IPEndPoint(serverAddress, 1413);

                var connectedEvent = new AutoResetEvent(false);
                var e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = endpoint;
                e.Completed += (sender, args) => connectedEvent.Set();

                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e))
                {
                    // Completed synchronously
                }

                connectedEvent.WaitOne();
                this.client = e.ConnectSocket;

                if (e.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)e.SocketError);
                }
            }
            catch (SocketException ex)
            {
                Debug.WriteLine("Failed to make TCP connection : {0}", ex);

                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }

                throw;
            }

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            this.protocolHandler = new TlsProtocolHandler(new NetworkStream(this.client));

            try
            {
                TivoTlsClient tivoTlsClient = new TivoTlsClient(CaptureTsnFromServerCert);

                this.protocolHandler.Connect(tivoTlsClient);
            }
            catch (IOException e)
            {
                Debug.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Debug.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                
                Debug.WriteLine("Authentication failed - closing the connection.");
                
                this.client.Dispose();
                this.client = null;

                this.protocolHandler.Close();
                this.protocolHandler = null;

                throw;
            }

            return protocolHandler.Stream;
        }

        private void CaptureTsnFromServerCert(string tsnFromCert)
        {
            string rawTsn = string.Join("", tsnFromCert.Split('-'));
            this.capturedTsn = string.Format("tsn:{0}", rawTsn);
        }
    }
}
