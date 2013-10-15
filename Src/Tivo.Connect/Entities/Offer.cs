//-----------------------------------------------------------------------
// <copyright file="Offer.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Offer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string OfferId { get; set; }

        public string ContentId { get; set; }
        public string ContentType { get; set; }
        public string CollectionId { get; set; }
        public string CollectionType { get; set; }

        public string PartnerContentId { get; set; }
        public string PartnerCollectionId { get; set; }

        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public string Subtitle { get; set; }

        public Channel Channel { get; set; }

        [JsonProperty("repeat")]
        public bool IsRepeat { get; set; }

        [JsonProperty("episodic")]
        public bool IsEpisodic { get; set; }

        [JsonProperty("hdtv")]
        public bool IsHdTv { get; set; }

        public bool IsEpisode { get; set; }
        public bool IsAdult { get; set; }

        public int? SeasonNumber { get; set; }

        [JsonProperty("episodeNum")]
        public List<int> EpisodeNumbers { get; set; }
        
        public DateTime StartTime { get; set; }

        [JsonProperty("duration")]
        public int DurationSeconds { get; set; }

        public List<ImageInfo> Images { get; set; }

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

        [JsonIgnore]
        public string EpisodeNumberText
        {
            get
            {
                if (this.EpisodeNumbers == null ||
                    this.EpisodeNumbers.Count == 0)
                {
                    return null;
                }

                var episodesAsStrings = this.EpisodeNumbers.Select(x => x.ToString());

                return string.Join(",", episodesAsStrings);
            }
        }

        [JsonIgnore]
        public TimeSpan Duration
        {
            get
            {
                return TimeSpan.FromSeconds(this.DurationSeconds);
            }
        }

        [JsonIgnore]
        public DateTime EndTime
        {
            get
            {
                return this.StartTime + this.Duration;
            }
        }
    }
}
