﻿using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class SubscriptionIdentifier : INotifyPropertyChanged
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

        public string SubscriptionId { get; set; }
        public string SubscriptionType { get; set; }
    }
}