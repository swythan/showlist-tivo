using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Person : INotifyPropertyChanged, IUnifiedItem
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
        public string NameId { get; set; }

        [JsonProperty("first")]
        public string FirstName { get; set; }

        [JsonProperty("middle")]
        public string MiddleName { get; set; }

        [JsonProperty("last")]
        public string LastName { get; set; }

        public string Sex { get; set; }

        public DateTime BirthDate { get; set; }
        public string BirthPlace { get; set; }

        [JsonProperty("imageUrl")]
        public string OriginalImageUrl { get; set; }

        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }

        public List<string> RoleForPersonId { get; set; }

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

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var sb = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(this.FirstName))
                {
                    sb.Append(this.FirstName);
                    sb.Append(" ");
                }

                if (!string.IsNullOrWhiteSpace(this.MiddleName))
                {
                    sb.Append(this.MiddleName);
                    sb.Append(" ");
                }

                if (!string.IsNullOrWhiteSpace(this.LastName))
                {
                    sb.Append(this.LastName);
                }

                return sb.ToString().Trim();
            }
        }
    }
}
