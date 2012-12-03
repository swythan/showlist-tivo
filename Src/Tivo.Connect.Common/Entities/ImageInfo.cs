using JsonFx.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class ImageInfo: INotifyPropertyChanged
    {
        private IDictionary<string, object> jsonSource;

        public ImageInfo(IDictionary<string, object> jsonSource)
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

        public string ImageId { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }

        public Uri ImageUrl { get; set; }
                
        private void SetupFromJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            if (this.jsonSource.ContainsKey("imageId"))
            {
                this.ImageId = (string)jsonSource["imageId"];
            }

            if (this.jsonSource.ContainsKey("width"))
            {
                this.Width = (int)jsonSource["width"];
            }

            if (this.jsonSource.ContainsKey("height"))
            {
                this.Height = (int)jsonSource["height"];
            }

            if (this.jsonSource.ContainsKey("imageUrl"))
            {
                var urlString = (string)jsonSource["imageUrl"];

                Uri originalUrl;
                if (Uri.TryCreate(urlString, UriKind.Absolute, out originalUrl))
                {
                    if (originalUrl.AbsolutePath.StartsWith("/images"))
                    {
                        var imagePath = originalUrl.AbsolutePath.Substring(7);
                        var vmImageHost = @"http://tivo-icdn.virginmedia.com/images-vm_production";

                        this.ImageUrl = new Uri(vmImageHost + imagePath);
                    }
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
