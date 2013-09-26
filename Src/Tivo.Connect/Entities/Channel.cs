//-----------------------------------------------------------------------
// <copyright file="Channel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Channel : INotifyPropertyChanged
    {
        public Channel()
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

        public string Affiliate { get; set; }
        public string CallSign { get; set; }
        public string ChannelId { get; set; }
        public int ChannelNumber { get; set; }
        public bool IsDigital { get; set; }
        public bool IsReceived { get; set; }
        public string Name { get; set; }
        public string PartnerStationId { get; set; }
        public string SourceType { get; set; }
        public string StationId { get; set; }
        public int? LogoIndex { get; set; }

        [JsonIgnore]
        public Uri LogoUrl
        {
            get
            {
                if (this.LogoIndex != null)
                {
                    const string urlFormat = @"http://tivo-icdn.virginmedia.com/images-production/static/logos/65x55/{0}.png";

                    int logoIdInUrl = this.LogoIndex.Value & 0xFFFF;
                    var logoUrlString = string.Format(urlFormat, logoIdInUrl);

                    Uri logoUrl;
                    if (Uri.TryCreate(logoUrlString, UriKind.Absolute, out logoUrl))
                    {
                        return logoUrl;
                    }
                }

                return null;
            }
        }
    }
}
