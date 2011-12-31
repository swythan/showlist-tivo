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
using JsonFx.Json;
using Tivo.Connect.Entities;

namespace Tivo.Connect
{
    public sealed class TivoConnection : IDisposable
    {
        TcpClient client = null;
        SslStream sslStream = null;

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
            this.sslStream = new SslStream(client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                this.sslStream.AuthenticateAsClient(serverAddress);
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
            SendAuthenticationRequest(mediaAccessKey);

            // Read auth response from the server.
            dynamic response = ReadMessage();

            if (response.type != "bodyAuthenticateResponse")
            {
                throw new FormatException("Expecting bodyAuthenticateResponse");
            }

            if (response.status == "success")
            {
                Debug.WriteLine("Authentication successfull");
                return;
            }

            throw new AuthenticationException(response.message as string);
        }

        public IEnumerable<RecordingFolderItem> GetMyShowsList()
        {
            SendGetMyShowsRequest();

            dynamic results = ReadMessage();

            var items = results.recordingFolderItem as IEnumerable<object>;

            return items.Select(item => RecordingFolderItem.Create(item));
        }

        public IEnumerable<RecordingFolderItem> GetFolderShowsList(Container parent)
        {
            SendGetFolderShowsRequest(parent.Id);

            dynamic results = ReadMessage();

            var items = results.recordingFolderItem as IEnumerable<object>;

            return items.Select(item => RecordingFolderItem.Create(item));
        }

        public void PlayShow(IndividualShow show)
        {
            SendPlayShowRequest(show.Id);

            dynamic results = ReadMessage();

        }

        private void SendRequest(string requestType, object body)
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

            var writer = new JsonWriter();
            string bodyText = writer.Write(body);

            var messageString = string.Format("MRPC/2 {0} {1}\r\n{2}{3}",
                Encoding.UTF8.GetByteCount(header.ToString()),
                Encoding.UTF8.GetByteCount(bodyText),
                header.ToString(),
                bodyText);

            this.sslStream.Write(Encoding.UTF8.GetBytes(messageString));
            this.sslStream.Flush();
        }

        private dynamic ReadMessage()
        {
            StreamReader reader = new StreamReader(this.sslStream, Encoding.UTF8);
            var preamble = reader.ReadLine();
            var preambleStrings = preamble.Split(' ');

            var headerBytes = int.Parse(preambleStrings[1]);
            var bodyBytes = int.Parse(preambleStrings[2]);

            var headerChars = new char[headerBytes];
            reader.ReadBlock(headerChars, 0, headerBytes);

            //var header = new String(headerChars);

            var bodyChars = new char[bodyBytes];
            reader.ReadBlock(bodyChars, 0, bodyBytes);

            var jsonReader = new JsonReader();
            return jsonReader.Read(new string(bodyChars));
        }

        private void SendAuthenticationRequest(string mediaAccessKey)
        {
            var body = new
            {
                type = "bodyAuthenticate",
                credential = new
                {
                    type = "makCredential",
                    key = mediaAccessKey
                }
            };

            SendRequest(body.type, body);
        }

        private void SendGetMyShowsRequest()
        {
            var body = new
            {
                type = "recordingFolderItemSearch",
                orderBy = new string[] { "startTime" },
                bodyId = "",
                //objectIdAndType = new string[] { "0" },
                //note = new string[] { "recordingForChildRecordingId" }

            };

            SendRequest(body.type, body);
        }

        private void SendGetFolderShowsRequest(string parentId)
        {
            var body = new
            {
                type = "recordingFolderItemSearch",
                orderBy = new string[] { "startTime" },
                bodyId = "",
                parentRecordingFolderItemId = parentId,
                //note = new string[] { "recordingForChildRecordingId" }

            };

            SendRequest(body.type, body);
        }

        private void SendPlayShowRequest(string showId)
        {
            var body = new
            {
                type = "uiNavigate",
                uri = "x-tivo:classicui:playback",
                parameters = new
                {
                    fUseTrioId = "true",
                    recordingId = showId,
                    fHideBannerOnEnter = "false"
                }
            };

            SendRequest(body.type, body);
        }


    }

}
