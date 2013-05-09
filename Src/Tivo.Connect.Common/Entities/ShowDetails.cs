using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class ShowDetails : INotifyPropertyChanged
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

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int? SeasonNumber { get; set; }
        public DateTime OriginalAirDate { get; set; }
        public string Description { get; set; }

        [JsonProperty("episodeNum")]
        public List<int> EpisodeNumbers { get; set; }

        [JsonProperty("image")]
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
    }
}
