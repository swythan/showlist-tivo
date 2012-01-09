using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JsonFx.Json;
using Tivo.Connect.Entities;
using JsonFx.Serialization;
using System.Threading;

namespace Tivo.Connect
{
    public abstract class TivoConnectionBase : IDisposable
    {
        Stream sslStream = null;
        int sessionId;
        int rpcId = 0;
        
        public TivoConnectionBase()
        {
            sessionId = new Random().Next(0x26c000, 0x27dc20);
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
            var authResponse = ReadMessage();

            if (((string)authResponse["type"]) != "bodyAuthenticateResponse")
            {
                throw new FormatException("Expecting bodyAuthenticateResponse");
            }

            if (((string)authResponse["status"]) != "success")
            {
                throw new Exception(authResponse["message"] as string);
            }

            Debug.WriteLine("Authentication successful");

            // Chcek for network control enabled
            SendOptStatusGetRequest();

            // Read status response from the server.
            var statusResponse = ReadMessage();

            if (((string)statusResponse["type"]) != "optStatusResponse")
            {
                throw new FormatException("Expecting optStatusResponse");
            }

            if (((string)statusResponse["optStatus"]) != "optIn")
            {
                throw new Exception("Network control not enabled");
            }
        }

        protected abstract Stream ConnectNetworkStream(string serverAddress);

        public IEnumerable<RecordingFolderItem> GetMyShowsList()
        {
            SendGetMyShowsRequest();

            var results = ReadMessage();

            var objectIds = (results["objectIdAndType"] as IEnumerable<string>).Select(id => int.Parse(id));

            var groups = objectIds.Select((id, ix) => new { id, Page = ix / 5 }).GroupBy(x => x.Page, x => x.id);

            foreach (var group in groups)
            {
                SendGetMyShowsItemDetailsRequest(group);

                var detailsResults = ReadMessage();

                var items = detailsResults["recordingFolderItem"] as IEnumerable<Dictionary<string, object>>;

                foreach (var item in items)
                {
                    yield return RecordingFolderItem.Create(item);
                }
            }
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
            header.AppendLine(string.Format("RpcId:{0}", Interlocked.Increment(ref this.rpcId)));
            header.AppendLine("SchemaVersion:7");
            header.AppendLine("Content-Type:application/json");
            header.AppendLine("RequestType:" + requestType);
            header.AppendLine("ResponseCount:single");
            header.AppendLine("BodyId:");
            header.AppendLine("X-ApplicationName:Quicksilver");
            header.AppendLine("X-ApplicationVersion:1.2");
            header.AppendLine(string.Format("X-ApplicationSessionId:0x{0:x}", this.sessionId));
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

            var header = new string(headerChars);

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

        private void SendOptStatusGetRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "optStatusGet" }
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
                { "format", "idSequence" },
                //objectIdAndType = new string[] { "0" },
                //note = new string[] { "recordingForChildRecordingId" }
            };

            SendRequest((string)body["type"], body);
        }

        private void SendGetMyShowsItemDetailsRequest(IEnumerable<int> itemIds)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", "" },
                { "objectIdAndType", itemIds.ToArray() },
                { "note", new string[] { "recordingForChildRecordingId" } }
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
