//-----------------------------------------------------------------------
// <copyright file="Category.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Category : INotifyPropertyChanged
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

        public string CategoryId { get; set; }
        public string Label { get; set; }

        public int AvailableContentCount { get; set; }
        public int DisplayRank { get; set; }
        
        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }

        //[JsonProperty("nChildren")]
        //public int ChildCategoryCount { get; set; }

        //[JsonProperty("topLevel")]
        //public bool IsTopLevel { get; set; }
    }
}
