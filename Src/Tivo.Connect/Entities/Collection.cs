﻿//-----------------------------------------------------------------------
// <copyright file="Collection.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Collection : INotifyPropertyChanged, IUnifiedItem
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
        //public string PartnerCollectionId { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        //public string DescriptionLanguage { get; set; }

        [JsonProperty("category")]
        public List<Category> Categories { get; set; }

        [JsonProperty("credit")]
        public List<Credit> Credits { get; set; }

        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }

        [JsonProperty("episodic")]
        public bool IsEpisodic { get; set; }

        [JsonProperty("hdtv")]
        public bool IsHdTv { get; set; }

        public bool HasLiveOffers { get; set; }
        public bool HasThreeDOffers { get; set; }
    }
}
