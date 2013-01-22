using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonFx.Json;
using Org.BouncyCastle.Crypto.Tls;
using Tivo.Connect.Entities;
using Wintellect.Sterling;

namespace Tivo.Connect
{
    public class TivoConnection : IDisposable
    {
        private ISterlingDatabaseInstance cacheDb;

        private readonly int sessionId;

        private Socket client;
        private TlsProtocolHandler protocolHandler;

        private Stream sslStream = null;
        private int lastRpcId = 0;

        private Task receiveTask;
        private Subject<Tuple<int, IDictionary<string, object>>> receiveSubject;
        private CancellationTokenSource receiveCancellationTokenSource;

        private string capturedTsn;

        public TivoConnection()
        {
            sessionId = new Random().Next(0x26c000, 0x27dc20);
        }

        public TivoConnection(ISterlingDatabaseInstance cacheDb)
            : this()
        {
            this.cacheDb = cacheDb;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
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

                if (receiveCancellationTokenSource != null)
                {
                    receiveCancellationTokenSource.Cancel();
                    receiveCancellationTokenSource = null;
                }
            }
        }

        public async Task Connect(IPAddress serverAddress, string mediaAccessKey)
        {
            this.capturedTsn = string.Empty;

            this.sslStream = await ConnectNetworkStream(new IPEndPoint(serverAddress, 1413));

            this.receiveSubject = new Subject<Tuple<int, IDictionary<string, object>>>();
            this.receiveCancellationTokenSource = new CancellationTokenSource();

            // Send authentication message to the TiVo. 
            var authTask = SendMakAuthenticationRequest(mediaAccessKey);

            // Start listening on the socket *after* the first send operation.
            // This stops errors occuring on WP7
            this.receiveTask = Task.Factory.StartNew(RpcReceiveThreadProc, this.receiveCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var authResponse = await authTask;

            if (((string)authResponse["type"]) != "bodyAuthenticateResponse")
            {
                if (((string)authResponse["type"]) == "error")
                {
                    throw new Exception(
                        string.Format("Authentication failed with error.\n Error code: {0}\nError text:{1}",
                            authResponse["code"],
                            authResponse["text"]));
                }
                else
                {
                    throw new FormatException("Expecting bodyAuthenticateResponse");
                }
            }

            if (((string)authResponse["status"]) != "success")
            {
                throw new Exception(authResponse["message"] as string);
            }

            Debug.WriteLine("Authentication successful");

            // Now check that network control is enabled
            var statusResponse = await SendOptStatusGetRequest();

            if (((string)statusResponse["type"]) != "optStatusResponse")
            {
                throw new FormatException("Expecting optStatusResponse");
            }

            if (((string)statusResponse["optStatus"]) != "optIn")
            {
                throw new Exception("Network control not enabled");
            }

            var bodyConfigResponse = await SendBodyConfigSearchRequest();

            if (((string)bodyConfigResponse["type"]) != "bodyConfigList")
            {
                throw new FormatException("Expecting bodyConfigList");
            }

            if (!bodyConfigResponse.ContainsKey("bodyConfig"))
            {
                throw new Exception("No bodyConfig element in bodyConfigList");
            }

            var bodyConfigs = (IDictionary<string, object>[])bodyConfigResponse["bodyConfig"];
            if (bodyConfigs.Length < 1)
            {
                throw new Exception("No bodyConfigs returned in bodyConfigList");
            }

            var bodyConfig = bodyConfigs[0];
            if (!bodyConfig.ContainsKey("bodyId"))
            {
                throw new Exception("No TSN returned in bodyConfig");
            }

            this.capturedTsn = (string)bodyConfig["bodyId"];
        }

        public async Task ConnectAway(string username, string password)
        {
            this.capturedTsn = string.Empty;

            this.sslStream = await ConnectNetworkStream(new DnsEndPoint(@"secure-tivo-api.virginmedia.com", 443));

            this.receiveSubject = new Subject<Tuple<int, IDictionary<string, object>>>();
            this.receiveCancellationTokenSource = new CancellationTokenSource();

            // Send authentication message to the TiVo. 
            var authTask = SendUsernameAndPasswordAuthenticationRequest(username, password);

            // Start listening on the socket *after* the first send operation.
            // This stops errors occuring on WP7
            this.receiveTask = Task.Factory.StartNew(RpcReceiveThreadProc, this.receiveCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var authResponse = await authTask;

            if (((string)authResponse["type"]) != "bodyAuthenticateResponse")
            {
                if (((string)authResponse["type"]) == "error")
                {
                    throw new Exception(
                        string.Format("Authentication failed with error.\n Error code: {0}\nError text:{1}",
                            authResponse["code"],
                            authResponse["text"]));
                }
                else
                {
                    throw new FormatException("Expecting bodyAuthenticateResponse");
                }
            }

            if (((string)authResponse["status"]) != "success")
            {
                throw new Exception(authResponse["message"] as string);
            }

            Debug.WriteLine("Authentication successful");

            if (string.IsNullOrEmpty(this.capturedTsn))
            {
                var deviceIds = authResponse["deviceId"] as IDictionary<string, object>[];

                if (deviceIds == null)
                {
                    throw new Exception("No TiVo devices associated with account");
                }

                // TODO : Select which TiVO
                this.capturedTsn = (string)deviceIds[0]["id"];
            }
        }

        private async Task<Stream> ConnectNetworkStream(EndPoint remoteEndPoint)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }

            // Create a TCP/IP connection to the TiVo.
            this.client = await ConnectSocketAsync(remoteEndPoint);

            Debug.WriteLine("Client connected.");

            try
            {
                // Create an SSL stream that will close the client's stream.
                var tivoTlsClient = new TivoTlsClient();

                this.protocolHandler = new TlsProtocolHandler(new NetworkStream(this.client));
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
        }

        private static async Task<Socket> ConnectSocketAsync(EndPoint remoteEndPoint)
        {
            var tcs = new TaskCompletionSource<Socket>();

            var args = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = remoteEndPoint,
            };

            args.Completed +=
                (sender, e) =>
                {
                    if (e.ConnectByNameError != null)
                    {
                        tcs.TrySetException(e.ConnectByNameError);
                    }
                    else
                    {
                        if (e.SocketError != SocketError.Success)
                        {
                            tcs.TrySetException(new SocketException((int)e.SocketError));
                        }
                        else
                        {
                            tcs.TrySetResult(e.ConnectSocket);
                        }
                    }
                };

            Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);

            return await tcs.Task;
        }

        private void RpcReceiveThreadProc()
        {
            try
            {
                while (true)
                {
                    this.receiveSubject.OnNext(ReadMessage());

                    if (this.receiveCancellationTokenSource.IsCancellationRequested)
                    {
                        this.receiveSubject.OnCompleted();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                this.receiveSubject.OnError(ex);
            }
        }

        public async Task<IEnumerable<RecordingFolderItem>> GetMyShowsList(Container parent, IProgress<RecordingFolderItem> progress)
        {
            var parentId = parent != null ? parent.Id : null;

            var folderShows = await SendGetFolderShowsRequest(parentId);

            var objectIds = ((IEnumerable<string>)folderShows["objectIdAndType"])
                             .Select(id => long.Parse(id));

            return await GetRecordingFolderItemsAsync(objectIds, 5, progress);
        }

        public async Task<ShowDetails> GetShowContentDetails(string contentId)
        {
            var detailsResults = await SendGetContentDetailsRequest(contentId);

            var content = (IEnumerable<IDictionary<string, object>>)detailsResults["content"];
            
            return new ShowDetails(content.First());
        }

        private async Task<IEnumerable<RecordingFolderItem>> GetRecordingFolderItemsAsync(IEnumerable<long> objectIds, int pageSize, IProgress<RecordingFolderItem> progress)
        {
            var groups = objectIds
                .Select((id, ix) => new { Id = id, Page = ix / pageSize })
                .GroupBy(x => x.Page, x => x.Id);

            var result = new List<RecordingFolderItem>(objectIds.Count());

            foreach (var group in groups)
            {
                var groupItems = await GetRecordingFolderItems(group).ConfigureAwait(false);
                            
                foreach (var item in groupItems)
                {
                    result.Add(item);
                    if (progress != null)
                    {
                        progress.Report(item);
                    }
                }        
            }

            return result;
        }

        private async Task<IEnumerable<RecordingFolderItem>> GetRecordingFolderItems(IEnumerable<long> objectIds)
        {
            var itemsInCache = Enumerable.Empty<RecordingFolderItem>();

            if (this.cacheDb != null)
            {
                var showsInCache = objectIds
                    .Join(this.cacheDb.Query<IndividualShow, long>(), id => id, tk => tk.Key, (id, tk) => tk.LazyValue.Value as RecordingFolderItem);

                var containersInCache = objectIds
                    .Join(this.cacheDb.Query<Container, long>(), id => id, tk => tk.Key, (id, tk) => tk.LazyValue.Value as RecordingFolderItem);

                itemsInCache = objectIds
                    .Join(showsInCache.Concat(containersInCache), id => id, item => item.ObjectId, (id, item) => item);
            }

            if (objectIds.Except(itemsInCache.Select(item => item.ObjectId)).Any())
            {
                var detailsResults = await SendGetMyShowsItemDetailsRequest(objectIds);
                var detailItems = (IEnumerable<IDictionary<string, object>>)detailsResults["recordingFolderItem"];
                var viewModels = detailItems.Select(detailItem => RecordingFolderItem.Create(detailItem));

                if (cacheDb != null)
                {
                    foreach (var item in viewModels)
                    {
                        this.cacheDb.Save(item.GetType(), item);
                    }

                    this.cacheDb.Flush();
                }

                return viewModels;
            }
            else
            {
                return itemsInCache;
            }
        }

        public Task PlayShow(string recordingId)
        {
            // TODO : Handle failure
            return SendPlayShowRequest(recordingId);
        }

        public async Task<ShowDetails> GetWhatsOn()
        {
            var whatsOnSearchBody = new Dictionary<string, object>()
            { 
                { "type", "whatsOnSearch" },
            };

            var whatsOnResponse = await SendRequest("whatsOnSearch", whatsOnSearchBody).ConfigureAwait(false);

            if (((string)whatsOnResponse["type"]) != "whatsOnList")
            {
                if (((string)whatsOnResponse["type"]) == "error")
                {
                    throw new Exception(
                        string.Format("WhatsOnSearch returned an error.\n Error code: {0}\nError text:{1}",
                            whatsOnResponse["code"],
                            whatsOnResponse["text"]));
                }
                else
                {
                    throw new FormatException("Expecting whatsOnList");
                }
            }

            var whatsOn = ((IEnumerable<IDictionary<string, object>>)whatsOnResponse["whatsOn"]).First();

            var contentId = (string)whatsOn["contentId"];

            return await GetShowContentDetails(contentId).ConfigureAwait(false);
        }

        private async Task<IDictionary<string, object>> SendRequest(string requestType, object body)
        {
            int requestRpcId = Interlocked.Increment(ref this.lastRpcId);

            int schemaVersion = 7;
            int appMajorVersion = 1;
            int appMinorVersion = 2;

            var header = new StringBuilder();
            header.AppendLine("Type:request");
            header.AppendLine(string.Format("RpcId:{0}", requestRpcId));
            header.AppendLine(string.Format("SchemaVersion:{0}", schemaVersion));
            header.AppendLine("Content-Type:application/json");
            header.AppendLine("RequestType:" + requestType);
            header.AppendLine("ResponseCount:single");

            if (!string.IsNullOrEmpty(this.capturedTsn))
            {
                header.AppendLine(string.Format("BodyId:{0}", this.capturedTsn));
            }

            header.AppendLine("X-ApplicationName:Quicksilver");
            header.AppendLine(string.Format("X-ApplicationVersion:{0}.{1}", appMajorVersion, appMinorVersion));
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

            return await this.receiveSubject
                .Where(message => message.Item1 == requestRpcId)
                .Select(message => message.Item2)
                .Take(1);
        }

        private Tuple<int, IDictionary<string, object>> ReadMessage()
        {
            StreamReader reader = new StreamReader(this.sslStream, Encoding.UTF8);
            var preamble = reader.ReadLine();
            var preambleStrings = preamble.Split(' ');

            var headerBytes = int.Parse(preambleStrings[1]);
            var bodyBytes = int.Parse(preambleStrings[2]);

            var headerChars = new char[headerBytes];
            reader.ReadBlock(headerChars, 0, headerBytes);

            var header = new string(headerChars);
            var headerReader = new StringReader(header);

            int rpcId = 0;
            while (true)
            {
                var line = headerReader.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith("RpcId", StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = line.Split(':');
                    if (tokens.Length > 1)
                    {
                        if (int.TryParse(tokens[1], out rpcId))
                        {
                            break;
                        }
                    }
                }
            }

            var bodyChars = new char[bodyBytes];
            reader.ReadBlock(bodyChars, 0, bodyBytes);

            var jsonReader = new JsonReader();
            string bodyJsonString = new string(bodyChars);
            IDictionary<string, object> body = jsonReader.Read<Dictionary<string, object>>(bodyJsonString);

            return Tuple.Create(rpcId, body);
        }

        private Task<IDictionary<string, object>> SendMakAuthenticationRequest(string mediaAccessKey)
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

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendUsernameAndPasswordAuthenticationRequest(string username, string password)
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "bodyAuthenticate" },
                { "credential",  
                    new Dictionary<string, object>
                    {
                        { "domain", "virgin" },
                        { "type", "usernameAndPasswordCredential" },
                        { "username", username },
                        { "password", password }
                    }                
                }
            };

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendOptStatusGetRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "optStatusGet" }
            };

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendBodyConfigSearchRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "bodyConfigSearch" }
            };

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendGetFolderShowsRequest(string parentId)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", this.capturedTsn },
                { "format", "idSequence" },
            };

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                body["parentRecordingFolderItemId"] = parentId;
            }

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendGetMyShowsItemDetailsRequest(IEnumerable<long> itemIds)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", this.capturedTsn },
                { "objectIdAndType", itemIds.ToArray() },
                { "note", new string[] { "recordingForChildRecordingId" } },
                //{ "responseTemplate", 
                //    new Dictionary<string, object>
                //    {
                //        { "type", "responseTemplate" },
                //        { "typeName", "recordingFolderItem" },
                //        { "fieldName", 
                //            new string[] 
                //            { 
                //                "recordingFolderItemId", 
                //                "childRecordingId", 
                //                "contentId", 
                //                "objectIdAndType", 
                //                "collectionType", 
                //                "title",
                //                "startTime",
                //                "folderItemCount",
                //                "folderType", 
                //            } 
                //        }
                //    }
                //}
            };

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendGetContentDetailsRequest(string contentId)
        {
            //            {
            //  "contentId": ["tivo:ct.306278"],
            //  "filterUnavailableContent": false,
            //  "bodyId": "tsn:XXXXXXXXXXXXXXX",
            //  "note": [
            //    "userContentForCollectionId",
            //    "broadbandOfferGroupForContentId",
            //    "recordingForContentId"
            //  ],
            //  "responseTemplate": [...see “Response Template”...],
            //  "imageRuleset": [...see “Image Ruleset”...],
            //  "type": "contentSearch",
            //  "levelOfDetail": "high"
            //}
            var body = new Dictionary<string, object>
            {
                { "contentId", new string[] { contentId } },
                { "filterUnavailableContent", false },
                { "bodyId", this.capturedTsn },
//                { "note", new string[] { "userContentForCollectionId", "broadbandOfferGroupForContentId", "recordingForContentId" } },
                { "note", new string[] { "recordingForContentId" } },
                { "type", "contentSearch" },
                { "levelOfDetail", "medium" },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "contentList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "content",
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "content" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "title",
                                    "subtitle",
                                    "description",
                                    "seasonNumber",
                                    "episodeNum",
                                    "originalAirdate",
                                    "image"
                                } 
                            }
                        }
                    }
                },
                { "imageRuleset", 
                    new Dictionary<string, object>
                    {
                        { "type", "imageRuleset" },
                        { "name", "tvLandscape" },
                        { "rule", 
                            new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "type", "imageRule" },
                                    { "ruleType", "exactMatchDimension" },
                                    { "imageType", new string[] {"showcaseBanner"} },
                                    { "width", 360 },
                                    { "height", 270 },
                                }
                            }
                        }
                    }
                }
            };

            return SendRequest((string)body["type"], body);
        }

        private Task<IDictionary<string, object>> SendPlayShowRequest(string showId)
        {
            var body =
                new Dictionary<string, object>
                { 
                    { "type", "uiNavigate" },
                    { "uri", "x-tivo:classicui:playback" },
                    { "parameters", 
                        new Dictionary<string, object> 
                        {
                            { "fUseTrioId", true },
                            { "recordingId", showId },
                            { "fHideBannerOnEnter", false }
                        }
                    }
                };

            return SendRequest((string)body["type"], body);
        }
    }
}
