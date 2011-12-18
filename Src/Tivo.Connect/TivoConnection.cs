using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;

namespace Tivo.Connect
{
    public sealed class TivoConnection : IDisposable
    {
        TcpClient client =null;

        public TivoConnection()
        {
            
        }

        public void Dispose()
        {
            if (this.client != null)
            {
                this.client.Close();
                Debug.WriteLine("Client closed.");

                this.client = null;
            }
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

        public void Connect(string serverAddress, string mediaAccessKey)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            // Create a TCP/IP connection to the TiVo.
            this.client = new TcpClient(serverAddress, 1413);

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            SslStream sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                sslStream.AuthenticateAsClient(serverAddress);
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

            // Send authentication message to the TiVo. 
            string authMessage = EncodeAuthenticationMessage(mediaAccessKey);
            sslStream.Write(Encoding.UTF8.GetBytes(authMessage));
            sslStream.Flush();
            
            // Read auth response from the server.
            string serverMessage = ReadMessage(sslStream);
            Debug.WriteLine("Server says: {0}", serverMessage);

            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { new DynamicJsonConverter() });

            dynamic obj = serializer.Deserialize(serverMessage, typeof(object));

            if (obj.type != "bodyAuthenticateResponse")
            {
                throw new FormatException("Expecting bodyAuthenticateResponse");
            }

            if (obj.status == "success")
            {
                Debug.WriteLine("Authentication successfull");
                return;
            }

            throw new AuthenticationException(obj.message as string);
        }

        private static string EncodeAuthenticationMessage(string mediaAccessKey)
        {
            string requestType = "bodyAuthenticate";

            var payload = new Dictionary<string, object>
            {
                {"type", requestType},
                {"credential", 
                    new Dictionary<string, object>
                    {
                        {"type", "makCredential"},
                        {"key", mediaAccessKey }
                    } 
                }
            };

            return EncodeMindRpcMessage(requestType, payload);
        }

        private static string EncodeMindRpcMessage(string requestType, Dictionary<string, object> payload)
        {
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

            string body = FormatJsonValues(payload);

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
