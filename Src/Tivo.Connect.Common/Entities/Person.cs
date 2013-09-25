//-----------------------------------------------------------------------
// <copyright file="Person.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
        //public string NameId { get; set; }

        [JsonProperty("first")]
        public string FirstName { get; set; }

        [JsonProperty("middle")]
        public string MiddleName { get; set; }

        [JsonProperty("last")]
        public string LastName { get; set; }

        public string Sex { get; set; }

        public string BirthPlace { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? DeathDate { get; set; }
        
        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }

        [JsonProperty("roleForPersonId")]
        public List<string> Roles { get; set; }

        [JsonProperty("contentSummaryForPersonId")]
        public List<ContentSummaryForPersonId> ContentSummary { get; set; }

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
