//-----------------------------------------------------------------------
// <copyright file="SubscriptionConflicts.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class SubscriptionConflicts : INotifyPropertyChanged
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

        public List<Offer> WillGet { get; set; }
        public List<Conflict> WillCancel { get; set; }
    }
}
