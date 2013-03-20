using System;
using System.Collections.Generic;
using System.ComponentModel;
using JsonFx.Json;

namespace Tivo.Connect.Entities
{
    public class Channel : INotifyPropertyChanged
    {
        private IDictionary<string, object> jsonSource;

        public Channel(IDictionary<string, object> jsonSource)
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

        public Uri LogoUrl
        {
            get
            {
                if (this.LogoIndex == null)
                {
                    return null;
                }

                return this.GetLogoUrlFromIndex(this.LogoIndex.Value);
            }
        }

        private void SetupFromJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            //if (jsonSource.ContainsKey("affiliate"))
            //{
            //    this.Affiliate = (string)jsonSource["affiliate"];
            //}

            this.CallSign = (string)jsonSource["callSign"];
            this.ChannelId = (string)jsonSource["channelId"];
            this.ChannelNumber = int.Parse((string)jsonSource["channelNumber"]);
            //this.Name = (string)jsonSource["name"];
            //this.PartnerStationId = (string)jsonSource["partnerStationId"];
            //this.SourceType = (string)jsonSource["sourceType"];
            //this.StationId = (string)jsonSource["stationId"];
            //this.IsDigital = (bool)jsonSource["isDigital"];
            //this.IsReceived = (bool)jsonSource["isReceived"];

            if (jsonSource.ContainsKey("logoIndex"))
            {
                this.LogoIndex = (int)jsonSource["logoIndex"];
            }
        }

        private Uri GetLogoUrlFromIndex(int index)
        {
            const string urlFormat = @"http://tivo-icdn.virginmedia.com/images-production/static/logos/65x55/{0}.png";

            int logoIdInUrl = index & 0xFFFF;
            var logoUrlString = string.Format(urlFormat, logoIdInUrl);

            Uri logoUrl;
            if (Uri.TryCreate(logoUrlString, UriKind.Absolute, out logoUrl))
            {
                return logoUrl;
            }

            return null;
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
