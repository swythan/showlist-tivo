﻿//-----------------------------------------------------------------------
// <copyright file="ImageInfo.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class ImageInfo: INotifyPropertyChanged
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

        public string ImageId { get; set; }
        public string ImageType { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        [JsonProperty("imageUrl")]
        public string OriginalImageUrl { get; set; }

        [JsonIgnore]
        public Uri ImageUrl
        {
            get
            {
                return ImageUrlMapper.Default.GetExternalImageUrl(this.OriginalImageUrl);
            }
        }
    }
}
