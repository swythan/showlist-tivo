//-----------------------------------------------------------------------
// <copyright file="TivoConnection.Common.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tivo.Connect.Entities;

namespace Tivo.Connect
{
    public class TivoConnection : IDisposable
    {
        private TivoNetworkSession tivoSession;

        private JsonSerializerSettings jsonSettings;
        private JsonSerializer jsonSerializer;

        private string capturedTsn;

        public TivoConnection()
        {
            this.jsonSettings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatString = "yyyy'-'MM'-'dd HH':'mm':'ss",
                Converters =
                {
                    new RecordingFolderItemCreator(), 
                    new UnifiedItemCreator(),
                }
            };

            this.jsonSerializer = JsonSerializer.Create(this.jsonSettings);
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
                if (this.tivoSession != null)
                {
                    this.tivoSession.Dispose();
                    this.tivoSession = null;
                }
            }
        }

        public string ConnectedTsn
        {
            get
            {
                if (this.capturedTsn == null)
                    return null;

                if (this.capturedTsn.StartsWith("tsn:"))
                {
                    return this.capturedTsn.Substring(4);
                }

                return this.capturedTsn;
            }
        }

        public async Task Connect(string serverAddress, string mediaAccessKey, TivoServiceProvider serviceProvider, ICertificateStore certificateStore)
        {
            this.capturedTsn = string.Empty;

            this.tivoSession = new TivoNetworkSession();

            var authTask = this.tivoSession.Connect(
                TivoEndPoint.CreateLocal(serverAddress, serviceProvider, certificateStore), 
                BuildMakAuthenticationRequest(mediaAccessKey));

            var authResponse = await authTask.ConfigureAwait(false);

            CheckResponse(authResponse, "bodyAuthenticateResponse", "Authentication");

            if (((string)authResponse["status"]) != "success")
            {
                throw new UnauthorizedAccessException((string)authResponse["message"]);
            }

            ////Debug.WriteLine("Authentication successful");

            // Now check that network control is enabled
            var statusResponse = await SendOptStatusGetRequest().ConfigureAwait(false);

            CheckResponse(statusResponse, "optStatusResponse", "OptStatusGet");

            var optResponseString = (string)statusResponse["optStatus"];
            if (optResponseString != "optIn" && optResponseString != "optNeutral")
            {
                throw new ActionNotSupportedException("Network control not enabled");
            }

            var bodyConfigResponse = await SendBodyConfigSearchRequest().ConfigureAwait(false);

            CheckResponse(bodyConfigResponse, "bodyConfigList", "BodyConfigSearch");

            if (bodyConfigResponse["bodyConfig"] == null)
            {
                throw new FormatException("No bodyConfig element in bodyConfigList");
            }

            var bodyConfigs = (JArray)bodyConfigResponse["bodyConfig"];
            if (bodyConfigs.Count < 1)
            {
                throw new FormatException("No bodyConfigs returned in bodyConfigList");
            }

            var bodyConfig = bodyConfigs[0];
            if (bodyConfig["bodyId"] == null)
            {
                throw new FormatException("No TSN returned in bodyConfig");
            }

            this.capturedTsn = (string)bodyConfig["bodyId"];

            var imageBaseUrls = await GetAppGlobalData("imageBaseUrl", 50).ConfigureAwait(false);

            ImageUrlMapper.Default.Initialise(imageBaseUrls);
        }

        public async Task<string> ConnectAway(string username, string password, TivoServiceProvider serviceProvider, ICertificateStore certificateStore)
        {
            this.capturedTsn = string.Empty;

            this.tivoSession = new TivoNetworkSession();

            Dictionary<string, object> authMessage;

            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    authMessage = BuildMmaAuthenticationRequest(username, password);
                    break;

                case TivoServiceProvider.VirginMediaUK:
                    authMessage = BuildUsernameAndPasswordAuthenticationRequest(username, password);
                    break;

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("service", "Must specify a valid service provider.");
            }

            var authTask = this.tivoSession.Connect(
                TivoEndPoint.CreateAway(serviceProvider, certificateStore),
                authMessage);

            var authResponse = await authTask.ConfigureAwait(false);

            CheckResponse(authResponse, "bodyAuthenticateResponse", "Authentication");

            if (((string)authResponse["status"]) != "success")
            {
                throw new UnauthorizedAccessException((string)authResponse["message"]);
            }

            ////Debug.WriteLine("Authentication successful");

            if (string.IsNullOrEmpty(this.capturedTsn))
            {
                var deviceIds = (JArray)authResponse["deviceId"];

                if (deviceIds == null ||
                    deviceIds.Count < 1)
                {
                    throw new InvalidOperationException("No TiVo devices associated with account");
                }

                // TODO : Select which TiVO
                this.capturedTsn = (string)deviceIds[0]["id"];
            }

            var imageBaseUrls = await GetAppGlobalData("imageBaseUrl", 50).ConfigureAwait(false);

            ImageUrlMapper.Default.Initialise(imageBaseUrls);

            return (string)authResponse["mediaAccessKey"];
        }

        public async Task<IList<AppGlobalData>> GetAppGlobalData(string appName, int count)
        {
            var response = await SendAppGlobalDataSearchRequest(appName, count).ConfigureAwait(false);

            CheckResponse(response, "appGlobalDataList", "appGlobalDataSearch");

            var results = response["appGlobalData"];
            if (results != null)
            {
                return results.ToObject<IList<AppGlobalData>>(this.jsonSerializer);
            }
            else
            {
                return new List<AppGlobalData>();
            }
        }

        public async Task<IEnumerable<RecordingFolderItem>> GetMyShowsList(Container parent, IProgress<RecordingFolderItem> progress)
        {
            var parentId = parent != null ? parent.Id : null;

            var folderShows = await SendGetFolderShowsRequest(parentId).ConfigureAwait(false);

            var objectIds = folderShows["objectIdAndType"].ToObject<IList<long>>(this.jsonSerializer);

            return await GetRecordingFolderItemsAsync(objectIds, 5, progress).ConfigureAwait(false);
        }

        public async Task<IEnumerable<long>> GetRecordingFolderItemIds(string parentId)
        {
            var folderShows = await SendGetFolderShowsRequest(parentId).ConfigureAwait(false);

            return folderShows["objectIdAndType"].ToObject<IList<long>>(this.jsonSerializer);
        }

        public async Task<ShowDetails> GetShowContentDetails(string contentId)
        {
            var detailsResults = await SendContentSearchRequest(contentId).ConfigureAwait(false);

            var content = (JArray)detailsResults["content"];

            return content.First().ToObject<ShowDetails>(this.jsonSerializer);
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

        public async Task<IEnumerable<RecordingFolderItem>> GetRecordingFolderItems(IEnumerable<long> objectIds)
        {
            var detailsResults = await SendGetMyShowsItemDetailsRequest(objectIds).ConfigureAwait(false);

            CheckResponse(detailsResults, "recordingFolderItemList", "recordingFolderItemSearch");
            var detailItems = detailsResults["recordingFolderItem"];

            return detailItems.ToObject<List<RecordingFolderItem>>(this.jsonSerializer);
        }

        public async Task<IList<long>> GetScheduledRecordingIds()
        {
            var results = await SendRecordingSearchIdRequest(new[] { "inProgress", "scheduled" }).ConfigureAwait(false);

            CheckResponse(results, "idSequence", "recordingSearch");

            var content = (JArray)results["objectIdAndType"];

            return content.ToObject<IList<long>>(this.jsonSerializer);
        }

        public async Task<IList<Recording>> GetScheduledRecordings(int offset, int count)
        {
            var response = await SendRecordingSearchRequest(new[] { "inProgress", "scheduled" }, offset, count).ConfigureAwait(false);

            CheckResponse(response, "recordingList", "recordingSearch");

            var results = response["recording"];
            if (results != null)
            {
                return results.ToObject<IList<Recording>>(this.jsonSerializer);
            }
            else
            {
                return new List<Recording>();
            }
        }

        public async Task<IList<Offer>> GetUpcomingOffersForCollection(string collectionId, int offset, int count)
        {
            var response = await SendUpcomingOfferSearchForCollectionIdRequest(collectionId, offset, count).ConfigureAwait(false);

            CheckResponse(response, "offerList", "offerSearch");

            var results = response["offer"];
            if (results != null)
            {
                return results.ToObject<IList<Offer>>(this.jsonSerializer);
            }
            else
            {
                return new List<Offer>();
            }
        }

        public async Task<IList<Offer>> GetUpcomingOffersForContent(string contentId, int offset, int count)
        {
            var response = await SendUpcomingOfferSearchForContentIdRequest(contentId, offset, count).ConfigureAwait(false);

            CheckResponse(response, "offerList", "offerSearch");

            var results = response["offer"];
            if (results != null)
            {
                return results.ToObject<IList<Offer>>(this.jsonSerializer);
            }
            else
            {
                return new List<Offer>();
            }
        }

        public async Task<Recording> GetRecordingDetails(string offerId)
        {
            var results = await SendRecordingSearchRequest(offerId).ConfigureAwait(false);

            CheckResponse(results, "recordingList", "recordingSearch");

            var content = (JArray)results["recording"];

            if (content == null ||
                !content.Any())
            {
                return null;
            }

            return content.First().ToObject<Recording>(this.jsonSerializer);
        }

        public async Task<Offer> GetOfferDetails(string offerId)
        {
            var detailsResults = await SendOfferSearchRequest(offerId).ConfigureAwait(false);

            CheckResponse(detailsResults, "offerList", "offerSearch");

            var content = (JArray)detailsResults["offer"];

            if (content == null ||
                !content.Any())
            {
                return null;
            }

            return content.First().ToObject<Offer>(this.jsonSerializer);
        }

        public async Task<Collection> GetCollectionDetails(string collectionId)
        {
            var detailsResults = await SendCollectionSearchRequest(collectionId).ConfigureAwait(false);

            CheckResponse(detailsResults, "collectionList", "collectionSearch");

            var content = (JArray)detailsResults["collection"];

            if (content == null ||
               !content.Any())
            {
                return null;
            }

            return content.First().ToObject<Collection>(this.jsonSerializer);
        }

        public async Task<Person> GetPersonDetails(string personId, bool includeContentSummary)
        {
            JObject detailsResults = await SendPersonSearchRequest(personId, includeContentSummary).ConfigureAwait(false);

            CheckResponse(detailsResults, "personList", "personSearch");

            var content = (JArray)detailsResults["person"];

            if (content == null ||
                !content.Any())
            {
                return null;
            }

            return content.First().ToObject<Person>(this.jsonSerializer);
        }

        public async Task<Person> GetBasicPersonDetails(string personId)
        {
            JObject detailsResults = await SendBasicPersonSearchRequest(personId).ConfigureAwait(false);

            CheckResponse(detailsResults, "personList", "personSearch");

            var content = (JArray)detailsResults["person"];

            if (content == null ||
                !content.Any())
            {
                return null;
            }

            return content.First().ToObject<Person>(this.jsonSerializer);
        }

        public async Task<IList<IUnifiedItem>> ExecuteUnifiedItemSearch(string keyword, int offset, int count)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return new List<IUnifiedItem>();
            }

            var response = await SendUnifiedItemSearchRequest(keyword, offset, count).ConfigureAwait(false);

            CheckResponse(response, "unifiedItemList", "unifiedItemSearch");

            var results = response["unifiedItem"];
            if (results != null)
            {
                return results.ToObject<IList<IUnifiedItem>>(this.jsonSerializer);
            }
            else
            {
                return new List<IUnifiedItem>();
            }
        }

        public async Task PlayShow(string recordingId)
        {
            var response = await SendPlayShowRequest(recordingId);

            CheckResponse(response, "success", "uiNavigate");
        }

        public async Task DeleteRecording(string recordingId)
        {
            var response = await SendDeleteRecordingRequest(recordingId);

            CheckResponse(response, "success", "recordingUpdate");
        }

        public async Task CancelRecording(string recordingId)
        {
            var response = await SendCancelRecordingRequest(recordingId);

            CheckResponse(response, "success", "recordingUpdate");
        }

        public async Task<SubscribeResult> ScheduleSingleRecording(string contentId, string offerId)
        {
            var response = await SendScheduleSingleRecordingRequest(contentId, offerId);

            CheckResponse(response, "subscribeResult", "subscribe");

            return response.ToObject<SubscribeResult>();
        }

        public async Task<ShowDetails> GetWhatsOn()
        {
            var whatsOnSearchBody = new Dictionary<string, object>()
            { 
                { "type", "whatsOnSearch" },
                { "bodyId", this.capturedTsn },
            };

            var whatsOnResponse = await this.tivoSession.SendRequest(whatsOnSearchBody).ConfigureAwait(false);

            CheckResponse(whatsOnResponse, "whatsOnList", "whatsOnSearch");

            var whatsOn = ((JArray)whatsOnResponse["whatsOn"]).First();

            var contentId = (string)whatsOn["contentId"];

            return await GetShowContentDetails(contentId).ConfigureAwait(false);
        }

        public async Task<List<Channel>> GetChannelsAsync()
        {
            var response = await SendChannelSearchRequest().ConfigureAwait(false);

            CheckResponse(response, "channelList", "channelSearch");

            if (response["channel"] != null)
            {
                return response["channel"].ToObject<List<Channel>>(this.jsonSerializer);
            }
            else
            {
                return new List<Channel>();
            }
        }

        public async Task<List<GridRow>> GetGridShowsAsync(DateTime minEndTime, DateTime maxStartTime, int anchorChannel, int count, int offset)
        {
            var response = await SendGridRowSearchRequest(minEndTime, maxStartTime, anchorChannel, count, offset).ConfigureAwait(false);

            CheckResponse(response, "gridRowList", "gridRowSearch");

            if (response["gridRow"] != null)
            {
                return response["gridRow"].ToObject<List<GridRow>>(this.jsonSerializer);
            }
            else
            {
                return new List<GridRow>();
            }
        }

        private static void CheckResponse(JObject response, string expectedType, string operationName)
        {
            var responseType = (string)response["type"];
            if (responseType == expectedType)
            {
                return;
            }

            if (responseType != "error")
            {
                throw new FormatException(string.Format("Expecting {0}, but got {1}", expectedType, responseType));
            }

            throw new TivoException((string)response["text"], (string)response["code"], operationName);
        }

        private Dictionary<string, object> BuildMakAuthenticationRequest(string mediaAccessKey)
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

            return body;
        }

        private Dictionary<string, object> BuildUsernameAndPasswordAuthenticationRequest(string username, string password)
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

            return body;
        }

        private Dictionary<string, object> BuildMmaAuthenticationRequest(string username, string password)
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "bodyAuthenticate" },
                { "credential",  
                    new Dictionary<string, object>
                    {
                        { "type", "mmaCredential" },
                        { "username", username },
                        { "password", password }
                    }                
                }
            };

            return body;
        }

        private Task<JObject> SendOptStatusGetRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "optStatusGet" }
            };

            return this.tivoSession.SendRequest(body);
        }

        private Task<JObject> SendBodyConfigSearchRequest()
        {
            var body = new Dictionary<string, object>()
            { 
                { "type", "bodyConfigSearch" }
            };

            return this.tivoSession.SendRequest(body);
        }

        private async Task<JObject> SendAppGlobalDataSearchRequest(string appName, int count)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "appGlobalDataSearch" },
                { "appName", appName},
                { "count", count },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private Task<JObject> SendGetFolderShowsRequest(string parentId)
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

            return this.tivoSession.SendRequest(body);
        }

        private Task<JObject> SendGetMyShowsItemDetailsRequest(IEnumerable<long> itemIds)
        {
            var body = new Dictionary<string, object>
            {
                { "type", "recordingFolderItemSearch" },
                { "orderBy", new string[] { "startTime" } },
                { "bodyId", this.capturedTsn },
                { "objectIdAndType", itemIds.ToArray() },
                { "note", new string[] { "recordingForChildRecordingId" } },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recordingFolderItemList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recordingFolderItem", 
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recordingFolderItem" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "objectIdAndType", 
                                    "collectionType", 
                                    "title",
                                    "childRecordingId", 
                                    "contentId", 
                                    "startTime",
                                    "recordingFolderItemId", 
                                    "folderItemCount",
                                    "folderType", 
                                    "recordingForChildRecordingId"
                                } 
                            }
                        },                      
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recording" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recordingId", 
                                    "state", 
                                    "offerId", 
                                    "contentId", 
                                    "channel",
                                    //"deletionPolicy", 
                                    //"suggestionScore", 
                                    "subscriptionIdentifier", 
                                    "isEpisode",
                                    "scheduledStartTime",
                                    "scheduledEndTime",
                                    //"hdtv",
                                    //"episodic",
                                    "title",
                                    "seasonNumber",
                                    "episodeNum",
                                    //"originalAirdate",
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "subscriptionIdentifier" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "subscriptionId", 
                                    "subscriptionType",
                                } 
                            }
                        }
                    }
                }
            };

            return this.tivoSession.SendRequest(body);
        }

        private async Task<JObject> SendContentSearchRequest(string contentId)
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
                //{ "responseTemplate", 
                //    new object[]
                //    {
                //        new Dictionary<string, object>
                //        {
                //            { "type", "responseTemplate" },
                //            { "typeName", "contentList" },
                //            { "fieldName", 
                //                new string[] 
                //                { 
                //                    "content",
                //                } 
                //            }
                //        },
                //        new Dictionary<string, object>
                //        {
                //            { "type", "responseTemplate" },
                //            { "typeName", "content" },
                //            { "fieldName", 
                //                new string[] 
                //                { 
                //                    "title",
                //                    "subtitle",
                //                    "description",
                //                    "seasonNumber",
                //                    "episodeNum",
                //                    "originalAirdate",
                //                    "image"
                //                } 
                //            }
                //        }
                //    }
                //},
                //{ "imageRuleset", 
                //    new Dictionary<string, object>
                //    {
                //        { "type", "imageRuleset" },
                //        { "name", "tvLandscape" },
                //        { "rule", 
                //            new object[]
                //            {
                //                new Dictionary<string, object>
                //                {
                //                    { "type", "imageRule" },
                //                    { "ruleType", "exactMatchDimension" },
                //                    { "imageType", new string[] {"showcaseBanner"} },
                //                    { "width", 360 },
                //                    { "height", 270 },
                //                }
                //            }
                //        }
                //    }
                //}
            };

            var response = await this.tivoSession.SendRequest(body).ConfigureAwait(false);
            return response;
        }

        private Task<JObject> SendPlayShowRequest(string showId)
        {
            var body =
                new Dictionary<string, object>
                { 
                    { "type", "uiNavigate" },
                    { "bodyId", this.capturedTsn },
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

            return this.tivoSession.SendRequest(body);
        }

        private Task<JObject> SendDeleteRecordingRequest(string recordingId)
        {
            return SendRecordingUpdateRequest(recordingId, "deleted");
        }

        private Task<JObject> SendCancelRecordingRequest(string recordingId)
        {
            return SendRecordingUpdateRequest(recordingId, "cancelled");
        }

        private Task<JObject> SendRecordingUpdateRequest(string recordingId, string newState)
        {
            var request =
                new Dictionary<string, object>
                { 
                    { "type", "recordingUpdate" },
                    { "bodyId", this.capturedTsn },
                    { "state", newState },
                    { "recordingId", recordingId }
                };

            return this.tivoSession.SendRequest(request);
        }

        private Task<JObject> SendScheduleSingleRecordingRequest(string contentId, string offerId)
        {
            // This seems to be ignoring the response template!
            var request =
                new Dictionary<string, object>
                { 
                    { "type", "subscribe" },
                    { "bodyId", this.capturedTsn },
                    { "recordingQuality", "best" },
                    { "showStatus", "rerunsAllowed" },
                    { "maxRecordings", 1 },
                    { "ignoreConflicts", "false" },
                    { "keepBehavior", "fifo" },
                    { "startTimePadding", 60 },
                    { "endTimePadding", 240 },
                    { "idSetSource", 
                        new Dictionary<string, object> 
                        {
                            { "type", "singleOfferSource" },
                            { "contentId", contentId },
                            { "offerId", offerId }
                        }
                    },
                    { "responseTemplate", 
                        new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "subscribeResult" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "subscription",
                                        "conflicts",
                                    } 
                                }
                            },                     
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "subscriptionIdentifier" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "subscriptionId", 
                                        "subscriptionType",
                                    } 
                                }
                            },
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "subscriptionConflicts" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "willCancel", 
                                        "willGet", 
                                    } 
                                }
                            },
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "conflict" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "reason", 
                                        "requestWinning", 
                                        "winningOffer", 
                                        "losingOffer", 
                                        "losingRecording", 
                                    } 
                                }
                            },
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "offer" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "offerId", 
                                        "contentId",
                                        "title", 
                                        "startTime", 
                                        "duration", 
                                    } 
                                }
                            },
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "recording" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "recordingId", 
                                        "state", 
                                        //"offerId", 
                                        //"contentId", 
                                        "channel",
                                        //"deletionPolicy", 
                                        //"suggestionScore", 
                                        //"subscriptionIdentifier", 
                                        //"isEpisode",
                                        "scheduledStartTime",
                                        "scheduledEndTime",
                                        //"hdtv",
                                        //"episodic",
                                        "title",
                                        //"originalAirdate",
                                    } 
                                }
                            },
                            new Dictionary<string, object>
                            {
                                { "type", "responseTemplate" },
                                { "typeName", "channel" },
                                { "fieldName", 
                                    new string[] 
                                    { 
                                        "channelId", 
                                        "channelNumber", 
                                        "callSign", 
                                        "logoIndex", 
                                    } 
                                }
                            },                 
                        }
                    }  
                };

            return this.tivoSession.SendRequest(request);
        }

        private async Task<JObject> SendChannelSearchRequest()
        {
            var request = new Dictionary<string, object>
            {
                { "type", "channelSearch" },
                { "bodyId", this.capturedTsn },
                { "noLimit", true},
                { "isReceived", true },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "channelList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channel",
                                } 
                            }
                        },                     
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "channel" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channelId", 
                                    "channelNumber", 
                                    "callSign", 
                                    "logoIndex", 
                                } 
                            }
                        },
                    }
                }  
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendGridRowSearchRequest(DateTime minEndTime, DateTime maxStartTime, int anchorChannel, int count, int offset)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "gridRowSearch" },
                { "bodyId", this.capturedTsn },
                { "orderBy", new string[] { "channelNumber"} },
                { "isReceived", true},
                { "minEndTime", minEndTime },
                { "maxStartTime", maxStartTime },
                { "count", count },
                { "offset", offset },
                { "anchorChannelIdentifier", 
                    new Dictionary<string, object>
                    {
                        { "type", "channelIdentifier"},
                        { "channelNumber", anchorChannel},
                        { "sourceType", "cable"},                    
                    }
                },
                { "levelOfDetail", "low" },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "gridRowList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "gridRow",
                                } 
                            }
                        },                     
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "gridRow" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channel", 
                                    "offer", 
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "channel" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channelId", 
                                    "channelNumber", 
                                    "callSign", 
                                    "logoIndex", 
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "offer" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "offerId", 
                                    "contentId",
                                    "title", 
                                    "startTime", 
                                    "duration", 
                                } 
                            }
                        }
                    }
                }  
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendRecordingSearchIdRequest(string[] states)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "recordingSearch" },
                { "bodyId", this.capturedTsn },
                { "state", states },
                { "noLimit", true },
                { "format", "idSequence" }
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendRecordingSearchRequest(string[] states, int offset, int count)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "recordingSearch" },
                { "bodyId", this.capturedTsn },
                { "state", states },
                { "offset", offset },
                { "count", count },
                { "levelOfDetail", "low" },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recordingList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recording",
                                    "isTop",
                                    "isBottom",
                                } 
                            }
                        },                     
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recording" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recordingId", 
                                    //"state", 
                                    "offerId", 
                                    //"contentId", 
                                    //"deletionPolicy", 
                                    //"suggestionScore", 
                                    //"subscriptionIdentifier", 
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "subscriptionIdentifier" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "subscriptionId", 
                                    "subscriptionType",
                                } 
                            }
                        }
                    }
                }  
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendRecordingSearchRequest(string recordingId)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "recordingSearch" },
                { "bodyId", this.capturedTsn },
                { "state", new[] { "inProgress", "scheduled" } },
                { "recordingId", recordingId },
                { "levelOfDetail", "low" },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recordingList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recording",
                                    "isTop",
                                    "isBottom",
                                } 
                            }
                        },                     
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "recording" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "recordingId", 
                                    "state", 
                                    "offerId", 
                                    "contentId", 
                                    "channel",
                                    //"deletionPolicy", 
                                    //"suggestionScore", 
                                    //"subscriptionIdentifier", 
                                    //"isEpisode",
                                    "scheduledStartTime",
                                    "scheduledEndTime",
                                    //"hdtv",
                                    //"episodic",
                                    "title",
                                    //"originalAirdate",
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "channel" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channelId", 
                                    "channelNumber", 
                                    "callSign", 
                                    "logoIndex", 
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "subscriptionIdentifier" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "subscriptionId", 
                                    "subscriptionType",
                                } 
                            }
                        }
                    }
                }  
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendOfferSearchRequest(string offerId)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "offerSearch" },
                { "bodyId", this.capturedTsn },
                { "searchable", true},
                { "receivedChannelsOnly", false},
                { "namespace", "refserver" },
                { "offerId", new[] { offerId } },
                { "note", new[] { "recordingForOfferId" } },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendUpcomingOfferSearchForCollectionIdRequest(string collectionId, int offset, int count)
        {
            var request = BuildUpcomingOfferSearchRequest(offset, count);

            request["collectionId"] = new[] { collectionId };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendUpcomingOfferSearchForContentIdRequest(string contentId, int offset, int count)
        {
            var request = BuildUpcomingOfferSearchRequest(offset, count);

            request["contentId"] = new[] { contentId };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private Dictionary<string, object> BuildUpcomingOfferSearchRequest(int offset, int count)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "offerSearch" },
                { "bodyId", this.capturedTsn },
                { "offset", offset },
                { "count", count },
                { "searchable", true},
                { "receivedChannelsOnly", false},
                { "namespace", "refserver" },
                { "note", new[] { "recordingForOfferId" } },
                { "minEndTime", DateTime.UtcNow },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "offerList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "offer",
                                    "isTop",
                                    "isBottom",
                                } 
                            }
                        },
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "offer" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "offerId",
                                    "contentId",
                                    "collectionId",
                                    "collectionType",
                                    "title",
                                    "subtitle",
                                    "startTime",
                                    "duration",
                                    "seasonNumber",
                                    "episodeNum",
                                    "episodic",
                                    "hdtv",
                                    "isAdult",
                                    "isEpisode",
                                    "repeat",
                                    "channel"
                                } 
                            }
                        }, 
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "channel" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "channelId", 
                                    "channelNumber", 
                                    "callSign", 
                                    "logoIndex", 
                                } 
                            }
                        },
                    }
                },
            };
            return request;
        }

        private async Task<JObject> SendCollectionSearchRequest(string collectionId)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "collectionSearch" },
                { "bodyId", this.capturedTsn },
                { "filterUnavailable", false },
                { "collectionId", new[] { collectionId } },
                { "note", 
                    new string[] 
                    { 
                        //"userContentForCollectionId", // Use this to get thumbs rating
                        //"broadcastOfferGroupForCollectionId", // Use this to get example offers
                        //"broadbandOfferGroupForCollectionId" // Not entirely sure what this gives you!
                    }
                },
                { "levelOfDetail", "high" },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendUnifiedItemSearchRequest(string keyword, int offset, int count)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "unifiedItemSearch" },
                { "bodyId", this.capturedTsn },
                { "keyword", keyword },
                { "count", count },
                { "offset", offset },
                { "numRelevantItems", 50 },
                { "orderBy", new[] { "relevance" } },
                { "searchable", true },
                { "mergeOverridingCollections", true },
                { "includeUnifiedItemType", new[] { "collection", "person" } },
                { "levelOfDetail", "high" },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendBasicPersonSearchRequest(string personId)
        {
            var request = new Dictionary<string, object>
            {
                { "type", "personSearch" },
                { "bodyId", this.capturedTsn },
                { "personId", new[] { personId } },
                { "note", 
                    new string[] 
                    { 
                        "roleForPersonId",
                    }
                },
                { "responseTemplate", 
                    new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "personList" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "person",
                                    "isTop",
                                    "isBottom",
                                } 
                            }
                        },                     
                        new Dictionary<string, object>
                        {
                            { "type", "responseTemplate" },
                            { "typeName", "person" },
                            { "fieldName", 
                                new string[] 
                                { 
                                    "personId", 
                                    "first", 
                                    "middle", 
                                    "last", 
                                    "roleForPersonId"
                                } 
                            }
                        },
                    }
                },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

        private async Task<JObject> SendPersonSearchRequest(string personId, bool includeContentSummary)
        {
            string[] notes;

            if (!includeContentSummary)
            {
                notes = new[] { "roleForPersonId" };
            }
            else
            {
                notes = new[] { "roleForPersonId", "contentSummaryForPersonId" };
            }

            var request = new Dictionary<string, object>
            {
                { "type", "personSearch" },
                { "bodyId", this.capturedTsn },
                { "personId", new[] { personId } },
                { "note", notes },
                { "levelOfDetail", "high" },
            };

            var response = await this.tivoSession.SendRequest(request).ConfigureAwait(false);
            return response;
        }

    }
}
