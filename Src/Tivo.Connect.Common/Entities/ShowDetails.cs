using System.Collections.Generic;
using System.ComponentModel;
using JsonFx.Json;
using System.Diagnostics;
using System;
using System.Globalization;

namespace Tivo.Connect.Entities
{
    public class ShowDetails : INotifyPropertyChanged
    {
        private IDictionary<string, object> jsonSource;

        public ShowDetails(IDictionary<string, object> jsonSource)
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

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public int? SeasonNumber { get; set; }
        public int? EpisodeNumber { get; set; }
        public DateTime OriginalAirDate { get; set; }
        public string Description { get; set; }

        private void SetupFromJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            if (this.jsonSource.ContainsKey("title"))
            {
                this.Title = (string)jsonSource["title"];
            }

            if (this.jsonSource.ContainsKey("subtitle"))
            {
                this.Subtitle = (string)jsonSource["subtitle"];
            }

            if (this.jsonSource.ContainsKey("description"))
            {
                this.Description = (string)jsonSource["description"];
            }

            if (this.jsonSource.ContainsKey("seasonNumber"))
            {
                this.SeasonNumber = (int)jsonSource["seasonNumber"];
            }

            if (this.jsonSource.ContainsKey("episodeNum"))
            {
                var episodeNumbers = jsonSource["episodeNum"] as int[];
                if (episodeNumbers != null)
                {
                    this.EpisodeNumber = episodeNumbers[0];
                }
            }

            if (this.jsonSource.ContainsKey("originalAirdate"))
            {
                var airDateString = (string)this.jsonSource["originalAirdate"];
                DateTime airDate;
                if (DateTime.TryParse(airDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out airDate))
                {
                    this.OriginalAirDate = airDate;
                }
                else
                {
                    Debug.WriteLine(
                        "Failed to parse originalAirDate: {0}", 
                        this.jsonSource["originalAirdate"]);
                }
            }
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
