using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Tivo.Connect
{
    public partial class TivoConnection
    {
        TcpClient client;

        protected virtual void DisposeSpecialized(bool disposing)
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

        private IObservable<Stream> ConnectNetworkStream(IPAddress serverAddress)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }


            // Create a TCP/IP connection to the TiVo.
            this.client = new TcpClient(serverAddress.ToString(), 1413);

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            var stream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                //                var clientCert = new X509Certificate(@"C:\source\SwythanTivoProject\Dev\Src\TivoAhoy.Phone\tivo_us.p12", "mpE7Qy8cSqdf");
                var clientCert = new X509Certificate(@"C:\source\SwythanTivoProject\Dev\Src\TivoAhoy.Phone\tivo_vm.p12", "R2N48DSKr2Cm");
                var clientCertCollection = new X509CertificateCollection()
                {
                    clientCert
                };

                stream.AuthenticateAsClient(serverAddress.ToString(), clientCertCollection, SslProtocols.Default, false);
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

            return Observable.Return(stream);
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
