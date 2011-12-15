using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Net;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Diagnostics;
using System.IO;

namespace Tivo.Connect
{
    public class TivoConnection
    {
        private static Hashtable certificateErrors = new Hashtable();

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return true;
        }

        public void Connect()
        {
            var serverName = "192.168.0.8";

            //var tivoEndpoint = new IPEndPoint(IPAddress.Parse("192.168.0.8"), 1413);

            // Create a TCP/IP client socket.
            // machineName is the host running the server application.
            TcpClient client = new TcpClient(serverName, 1413);

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            SslStream sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                sslStream.AuthenticateAsClient(serverName);
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

            // Encode a test message into a byte array.
            string builtMessage = EncodeMindRpcMessage();
            string hardMessage = GetDefaultMindRpcMessage();

            // Send hello message to the server. 
            sslStream.Write(Encoding.UTF8.GetBytes(builtMessage));
            sslStream.Flush();


            // Read message from the server.
            string serverMessage = ReadMessage(sslStream);
            Debug.WriteLine("Server says: {0}", serverMessage);
            //// Close the client connection.
            //client.Close();
            //Debug.WriteLine("Client closed.");
        }

        private string GetDefaultMindRpcMessage()
        {
            string messageString = @"MRPC/2 225 85
Type:request
RpcId:20
SchemaVersion:7
Content-Type:application/json
RequestType:bodyAuthenticate
ResponseCount:single
BodyId:
X-ApplicationName:Quicksilver
X-ApplicationVersion:1.2
X-ApplicationSessionId:0x27dc20

{""type"":""bodyAuthenticate"",""credential"":{""type"":""makCredential"",""key"":""9837127953""}}
";

            return messageString;
        }

        private static string EncodeMindRpcMessage()
        {
            string requestType = "bodyAuthenticate";
            string makString = "9837127953";

            var payload = new Dictionary<string, object>
            {
                {"type", requestType},
                {"credential", 
                    new Dictionary<string, object>
                    {
                        {"type", "makCredential"},
                        {"key", makString }
                    } 
                }
            };

            string body = FormatJsonValues(payload);

            var header = new StringBuilder();
            header.AppendLine("Type:request");
            header.AppendLine("RpcId:20");
            header.AppendLine("SchemaVersion:7");
            header.AppendLine("Content-Type:application/json");
            header.AppendLine("RequestType:" + requestType);
            header.AppendLine("ResponseCount:single");
            header.AppendLine("BodyId:");
            header.AppendLine("X-ApplicationName:Quicksilver");
            header.AppendLine("X-ApplicationVersion:1.2");
            header.AppendLine("X-ApplicationSessionId:0x27dc20");
            header.AppendLine();

            var messageString = string.Format("MRPC/2 {0} {1}\r\n{2}{3}",
                Encoding.UTF8.GetByteCount(header.ToString()),
                Encoding.UTF8.GetByteCount(body),
                header.ToString(),
                body);

            return messageString;
        }

        private static string FormatJsonValues(IDictionary<string, object> credential)
        {
            var itemStrings = credential.Select(kv => FormatJsonKeyValuePair(kv));

            return string.Format("{0}{1}{2}", "{", string.Join(",", itemStrings), "}");
        }

        private static string FormatJsonKeyValuePair(KeyValuePair<string, object> kv)
        {
            var valueAsDictionary = kv.Value as IDictionary<string, object>;

            if (valueAsDictionary != null)
            {
                return string.Format("\"{0}\":{1}", kv.Key, FormatJsonValues(valueAsDictionary));
            }

            return string.Format("\"{0}\":\"{1}\"", kv.Key, kv.Value);
        }

        static string ReadMessage(SslStream sslStream)
        {
            StreamReader reader = new StreamReader(sslStream, Encoding.UTF8);
            var preamble = reader.ReadLine();
            var preambleStrings = preamble.Split(' ');

            var headerBytes = int.Parse(preambleStrings[1]);
            var bodyBytes = int.Parse(preambleStrings[2]);

            var headerChars = new char[headerBytes];
            reader.ReadBlock(headerChars, 0, headerBytes);

            var header = new String(headerChars);

            var bodyChars = new char[bodyBytes];
            reader.ReadBlock(bodyChars, 0, bodyBytes);

            var body = new string(bodyChars);

            return body;            
        }
    }
}
