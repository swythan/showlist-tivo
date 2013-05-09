using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Conflict : INotifyPropertyChanged
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
        
        public string Reason { get; set; }
        public bool RequestWinning { get; set; }
        
        public List<Offer> WinningOffer { get; set; }
        public List<Offer> LosingOffer { get; set; }
        public List<Recording> LosingRecording { get; set; }
    }
}
