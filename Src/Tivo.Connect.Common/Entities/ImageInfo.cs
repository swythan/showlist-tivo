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
