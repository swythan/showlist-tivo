using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using JsonFx.Json;

namespace Tivo.Connect.Entities
{
    public class Offer : INotifyPropertyChanged
    {
        private IDictionary<string, object> jsonSource;

        public Offer(IDictionary<string, object> jsonSource)
        {
            SetupFromJson(jsonSource);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string ContentId { get; set; }
        public string ContentType { get; set; }
        public string CollectionId { get; set; }
        public string CollectionType { get; set; }
        public string OfferId { get; set; }

        public string PartnerContentId { get; set; }
        public string PartnerCollectionId { get; set; }
        
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public string Subtitle { get; set; }
        
        public Channel Channel { get; set; }
        
        public bool IsRepeat { get; set; }
        public bool IsEpisodic { get; set; }
        public bool IsHdTv { get; set; }
        public bool IsEpisode { get; set; }
        public bool IsAdult { get; set; }

        public int? EpisodeNum { get; set; }
        public int? SeasonNumber { get; set; }

        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        
        private void SetupFromJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            this.ContentId = (string)jsonSource["contentId"];
            this.ContentType = (string)jsonSource["contentType"];
            this.CollectionId = (string)jsonSource  ["collectionId"];
            this.CollectionType = (string)jsonSource["collectionType"];
            this.PartnerContentId = (string)jsonSource   ["partnerContentId"];
            this.PartnerCollectionId = (string)jsonSource["partnerCollectionId"];

            this.Title =      (string)jsonSource["title"];
            
            if (jsonSource.ContainsKey("shortTitle"))
            {
                this.ShortTitle = (string)jsonSource["shortTitle"];
            }

            if (jsonSource.ContainsKey("subtitle"))
            {
                this.Subtitle = (string)jsonSource["subtitle"];
            }

            if (this.jsonSource.ContainsKey("channel"))
            {
                this.Channel = new Channel((IDictionary<string, object>) jsonSource["channel"]);
            }

            if (this.jsonSource.ContainsKey("repeat"))
            {
                this.IsRepeat = (bool)jsonSource["repeat"];
            }

            this.IsEpisodic = (bool)jsonSource["episodic"];
            this.IsHdTv = (bool)jsonSource["hdtv"];
            this.IsEpisode = (bool)jsonSource["isEpisode"];
            this.IsAdult = (bool)jsonSource["isAdult"];

            if (this.jsonSource.ContainsKey("episodeNum"))
            {
                this.EpisodeNum = ((IEnumerable<int>)this.jsonSource["episodeNum"]).First();
            }

            if (this.jsonSource.ContainsKey("seasonNumber"))
            {
                this.SeasonNumber = (int)this.jsonSource["seasonNumber"];
            }

            if (jsonSource.ContainsKey("startTime"))
            {
                var startTimeString = (string)jsonSource["startTime"];
                DateTime startTime;
                if (DateTime.TryParse(startTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime))
                {
                    this.StartTime = startTime;
                }
                else
                {
                    Debug.WriteLine(
                        "Failed to parse startTime: {0}",
                        jsonSource["startTime"]);
                }
            }

            this.Duration = TimeSpan.FromSeconds((int)this.jsonSource["duration"]);
        }

        public string JsonText
        {
            get
            {
                if (this.jsonSource == null)
                    return string.Empty;

                var writer = new JsonWriter();
                writer.Settings.PrettyPrint = true;

                return writer.Write(this.jsonSource);
            }
        }
    }
}
