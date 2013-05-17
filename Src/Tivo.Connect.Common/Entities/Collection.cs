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

        public List<Category> category { get; set; }
        public List<Credit> credit { get; set; }

        [JsonProperty("episodic")]
        public bool IsEpisodic { get; set; }

        [JsonProperty("hdtv")]
        public bool IsHdTv { get; set; }

        public bool HasLiveOffers { get; set; }
        public bool HasThreeDOffers { get; set; }
    }
}
