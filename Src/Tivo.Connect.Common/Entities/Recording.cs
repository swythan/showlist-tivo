using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Recording : INotifyPropertyChanged
    {
        public Recording()
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


        public string recordingId { get; set; }
        public string state { get; set; }
        public string offerId { get; set; }
        public string contentId { get; set; }
        public string deletionPolicy { get; set; }
        public int suggestionScore { get; set; }

        public List<SubscriptionIdentifier> subscriptionIdentifier { get; set; }
    }
}
