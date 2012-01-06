using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JsonFx.Json;
using Tivo.Connect.Entities;
using JsonFx.Serialization;

namespace Tivo.Connect
{
    public abstract class TivoConnectionBase : IDisposable
    {
        Stream sslStream = null;

        public TivoConnectionBase()
        {

        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }


        public void Connect(string serverAddress, string mediaAccessKey)
        {
            this.sslStream = ConnectNetworkStream(serverAddress);

            // Send authentication message to the TiVo. 
            SendAuthenticationRequest(mediaAccessKey);

            // Read auth response from the server.
            var response = ReadMessage();

            if (((string)response["type"]) != "bodyAuthenticateResponse")
            {
                throw new FormatException("Expecting bodyAuthenticateResponse");
            }

            if (((string)response["status"]) == "success")
            {
                Debug.WriteLine("Authentication successfull");
                return;
            }

            throw new Exception(response["message"] as string);
        }

        protected abstract Stream ConnectNetworkStream(string serverAddress);

        public IEnumerable<RecordingFolderItem> GetMyShowsList()
        {
            SendGetMyShowsRequest();

            var results = ReadMessage();

            var items = results["recordingFolderItem"] as IEnumerable<Dictionary<string, object>>;

            return items.Select(item => RecordingFolderItem.Create(item));
        }

        public IEnumerable<RecordingFolderItem> GetFolderShowsList(Container parent)
        {
            SendGetFolderShowsRequest(parent.Id);

            var results = ReadMessage();

            var items = results["recordingFolderItem"] as IEnumerable<Dictionary<string, object>>;

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

            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            this.sslStream.Write(messageBytes, 0, messageBytes.Length);
            this.sslStream.Flush();
        }

        private Dictionary<string, object> ReadMessage()
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
            return jsonReader.Read<Dictionary<string, object>>(new string(bodyChars));
        }

        private void SendAuthenticationRequest(string mediaAccessKey)
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "bodyAuthenticate" },
                { "credential",  
                    new Dictionary<string, object>
                    {
                        { "type", "makCredential" },
                        { "key" , mediaAccessKey }
                    }
                }
            };

            SendRequest((string)body["type"], body);
        }

        private void SendGetMyShowsRequest()
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", "" },
                //objectIdAndType = new string[] { "0" },
                //note = new string[] { "recordingForChildRecordingId" }
            };

            SendRequest((string)body["type"], body);
        }

        private void SendGetFolderShowsRequest(string parentId)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", "" },
                { "parentRecordingFolderItemId", parentId },
                //note = new string[] { "recordingForChildRecordingId" }

            };

            SendRequest((string)body["type"], body);
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
