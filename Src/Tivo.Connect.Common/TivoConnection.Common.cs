using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonFx.Json;
using Tivo.Connect.Entities;
using Wintellect.Sterling;

namespace Tivo.Connect
{
    public partial class TivoConnection : IDisposable
    {
        private ISterlingDatabaseInstance cacheDb;

        private readonly int sessionId;
        
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
            DisposeSpecialized(disposing);
            if (disposing)
            {
                if (receiveCancellationTokenSource != null)
                {
                    receiveCancellationTokenSource.Cancel();
                    receiveCancellationTokenSource = null;
                }
            }
        }

        public IObservable<Unit> Connect(IPAddress serverAddress, string mediaAccessKey)
        {
            this.capturedTsn = string.Empty;

            return ConnectNetworkStream(serverAddress)
                .SelectMany(
                    stream =>
                    {
                        this.sslStream = stream;

                        this.receiveSubject = new Subject<Tuple<int, IDictionary<string, object>>>();
                        this.receiveCancellationTokenSource = new CancellationTokenSource();

                        // Send authentication message to the TiVo. 
                        var result = SendAuthenticationRequest(mediaAccessKey);

                        // Start listening on the socket *after* the first send operation.
                        // This stops errors occuring on WP7
                        this.receiveTask = Task.Factory.StartNew(RpcReceiveThreadProc, this.receiveCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                        return result;
                    })
                .SelectMany(
                    authResponse =>
                    {
                        if (((string)authResponse["type"]) != "bodyAuthenticateResponse")
                        {
                            throw new FormatException("Expecting bodyAuthenticateResponse");
                        }

                        if (((string)authResponse["status"]) != "success")
                        {
                            throw new Exception(authResponse["message"] as string);
                        }

                        Debug.WriteLine("Authentication successful");

                        // Now check that network control is enabled
                        return SendOptStatusGetRequest();
                    })
                .Select(
                    statusResponse =>
                    {
                        if (((string)statusResponse["type"]) != "optStatusResponse")
                        {
                            throw new FormatException("Expecting optStatusResponse");
                        }

                        if (((string)statusResponse["optStatus"]) != "optIn")
                        {
                            throw new Exception("Network control not enabled");
                        }

                        return Unit.Default;
                    }); 
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


        public IObservable<RecordingFolderItem> GetMyShowsList(Container parent)
        {
            var parentId = parent != null ? parent.Id : null;

            return SendGetFolderShowsRequest(parentId)
                .SelectMany(results =>
                    {
                        var objectIds = (results["objectIdAndType"] as IEnumerable<string>)
                            .Select(id => long.Parse(id));

                        return GetRecordingFolderItems(objectIds, 5);
                    });
        }

        public IObservable<ShowDetails> GetShowContentDetails(string contentId)
        {
            var result = SendGetContentDetailsRequest(contentId)
                .Select(detailsResults => ((IEnumerable<IDictionary<string, object>>)detailsResults["content"]).ToObservable())
                .Concat()
                .Select(x => new ShowDetails(x));

            return result;
        }

        private IObservable<RecordingFolderItem> GetRecordingFolderItems(IEnumerable<long> objectIds, int pageSize)
        {
            var groups = objectIds
                .Select((id, ix) => new { Id = id, Page = ix / pageSize })
                .GroupBy(x => x.Page, x => x.Id);

            return groups
                .Select(group => GetRecordingFolderItems(group))
                .Concat();
        }

        private IObservable<RecordingFolderItem> GetRecordingFolderItems(IEnumerable<long> objectIds)
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
                var result = SendGetMyShowsItemDetailsRequest(objectIds)
                    .Select(detailsResults => ((IEnumerable<IDictionary<string, object>>)detailsResults["recordingFolderItem"]).ToObservable())
                    .Concat()
                    .Select(detailItem => RecordingFolderItem.Create(detailItem));

                if (cacheDb != null)
                {
                    result.Subscribe(item => this.cacheDb.Save(item.GetType(), item), () => this.cacheDb.Flush());
                }

                return result;
            }
            else
            {
                return itemsInCache.ToObservable();
            }
        }

        public IObservable<Unit> PlayShow(string recordingId)
        {
            // TODO : Handle failure
            return SendPlayShowRequest(recordingId).Select(result => Unit.Default);
        }

        private IObservable<IDictionary<string, object>> SendRequest(string requestType, object body)
        {
            var requestRpcId = Interlocked.Increment(ref this.lastRpcId);

            var header = new StringBuilder();
            header.AppendLine("Type:request");
            header.AppendLine(string.Format("RpcId:{0}", requestRpcId));
            header.AppendLine("SchemaVersion:7");
            header.AppendLine("Content-Type:application/json");
            header.AppendLine("RequestType:" + requestType);
            header.AppendLine("ResponseCount:single");
            header.AppendLine(string.Format("BodyId:{0}", this.capturedTsn));
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

            return this.receiveSubject
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

        private IObservable<IDictionary<string, object>> SendAuthenticationRequest(string mediaAccessKey)
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

        private IObservable<IDictionary<string, object>> SendOptStatusGetRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "optStatusGet" }
            };

            return SendRequest((string)body["type"], body);
        }

        private IObservable<IDictionary<string, object>> SendGetFolderShowsRequest(string parentId)
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

        private IObservable<IDictionary<string, object>> SendGetMyShowsItemDetailsRequest(IEnumerable<long> itemIds)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", this.capturedTsn },
                { "objectIdAndType", itemIds.ToArray() },
                { "note", new string[] { "recordingForChildRecordingId" } }
            };

            return SendRequest((string)body["type"], body);
        }

        private IObservable<IDictionary<string, object>> SendGetContentDetailsRequest(string contentId)
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
                { "levelOfDetail", "high" },
            };

            return SendRequest((string)body["type"], body);
        }

        private IObservable<IDictionary<string, object>> SendPlayShowRequest(string showId)
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
