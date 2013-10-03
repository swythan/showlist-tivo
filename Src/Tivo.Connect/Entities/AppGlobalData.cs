//-----------------------------------------------------------------------
// <copyright file="AppGlobalData.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel;

namespace Tivo.Connect.Entities
{
    public class AppGlobalData : INotifyPropertyChanged
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

        public string AppName { get; set; }
        public string KeyName { get; set; }
        public string LevelOfDetail { get; set; }
        public string UpdateDate { get; set; }
        public string Value { get; set; }
    }
}
