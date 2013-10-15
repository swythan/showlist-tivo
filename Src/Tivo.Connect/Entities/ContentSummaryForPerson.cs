//-----------------------------------------------------------------------
// <copyright file="ContentSummaryForPerson.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class ContentSummaryForPersonId : INotifyPropertyChanged
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

        public string CollectionId { get; set; }
        public string CollectionType { get; set; }

        public string Title { get; set; }
        //public string Subtitle { get; set; }
        //public string Description { get; set; }

        public string ContentId { get; set; }
        public string ContentType { get; set; }

        //[JsonProperty("category")]
        //public List<Category> Categories { get; set; }

        //[JsonProperty("credit")]
        //public List<Credit> Credits { get; set; }

        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }

        public int MovieYear { get; set; }

        //public int? SeasonNumber { get; set; }

        //[JsonProperty("episodeNum")]
        //public List<int> EpisodeNumbers { get; set; }

        //public DateTime OriginalAirdate { get; set; }

        //[JsonIgnore]
        //public int? EpisodeNumber
        //{
        //    get
        //    {
        //        if (this.EpisodeNumbers == null ||
        //            this.EpisodeNumbers.Count == 0)
        //        {
        //            return null;
        //        }

        //        return this.EpisodeNumbers[0];
        //    }
        //}       
    }
}
