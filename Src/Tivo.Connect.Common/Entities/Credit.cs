using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Credit : INotifyPropertyChanged
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

        public string PersonId { get; set; }

        [JsonProperty("first")]
        public string FirstName { get; set; }

        [JsonProperty("middle")]
        public string MiddleName { get; set; }

        [JsonProperty("last")]
        public string LastName { get; set; }

        public string Role { get; set; }

        [JsonProperty("imageUrl")]
        public string OriginalImageUrl { get; set; }

        [JsonIgnore]
        public Uri ImageUrl
        {
            get
            {
                if (this.OriginalImageUrl != null)
                {
                    Uri originalUrl;
                    if (Uri.TryCreate(this.OriginalImageUrl, UriKind.Absolute, out originalUrl))
                    {
                        if (originalUrl.AbsolutePath.StartsWith("/images"))
                        {
                            var imagePath = originalUrl.AbsolutePath.Substring(7);
                            var vmImageHost = @"http://tivo-icdn.virginmedia.com/images-vm_production";

                            return new Uri(vmImageHost + imagePath);
                        }
                    }
                }

                return null;
            }
        }
    }
}
