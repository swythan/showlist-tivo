using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Recording : INotifyPropertyChanged
    {
        public Recording()
        {
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


        public string RecordingId { get; set; }
        public string State { get; set; }
        public string OfferId { get; set; }
        public string ContentId { get; set; }
        public string DeletionPolicy { get; set; }
        public int SuggestionScore { get; set; }

        public Channel Channel { get; set; }
        public List<SubscriptionIdentifier> SubscriptionIdentifier { get; set; }

        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }

        [JsonProperty("repeat")]
        public bool IsRepeat { get; set; }

        [JsonProperty("episodic")]
        public bool IsEpisodic { get; set; }

        [JsonProperty("hdtv")]
        public bool IsHdTv { get; set; }

        public bool IsEpisode { get; set; }

        public string Title { get; set; }
        public DateTime OriginalAirdate { get; set; }

        public int? SeasonNumber { get; set; }

        [JsonProperty("episodeNum")]
        public List<int> EpisodeNumbers { get; set; }

        [JsonIgnore]
        public int? EpisodeNumber
        {
            get
            {
                if (this.EpisodeNumbers == null ||
                    this.EpisodeNumbers.Count == 0)
                {
                    return null;
                }

                return this.EpisodeNumbers[0];
            }
        }


    }
}
