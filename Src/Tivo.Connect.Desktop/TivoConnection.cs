using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Tivo.Connect
{
    public partial class TivoConnection
    {
        TcpClient client;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.client != null)
                {
                    this.client.Close();
                    Debug.WriteLine("Client closed.");

                    this.client = null;
                }
            }
        }

        private Stream ConnectNetworkStream(string serverAddress)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }


            // Create a TCP/IP connection to the TiVo.
            this.client = new TcpClient(serverAddress, 1413);

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            var stream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                stream.AuthenticateAsClient(serverAddress);
            }
            catch (AuthenticationException e)
            {
                Debug.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Debug.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Debug.WriteLine("Authentication failed - closing the connection.");
                client.Close();

                throw;
            }

            return stream;
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            // Allow this client to communicate with unauthenticated servers.
            return true;
        }
    }
}
