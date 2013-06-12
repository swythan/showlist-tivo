using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Credit : INotifyPropertyChanged
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

        [JsonProperty("first")]
        public string FirstName { get; set; }

        [JsonProperty("middle")]
        public string MiddleName { get; set; }

        [JsonProperty("last")]
        public string LastName { get; set; }

        [JsonProperty("role")]
        public string OriginalRole { get; set; }

        public string CharacterName { get; set; }

        [JsonProperty("image")]
        public List<ImageInfo> Images { get; set; }
        
        [JsonIgnore]
        public string Role
        {
            get
            {
                return this.OriginalRole.SplitCamelCase().UppercaseFirst();
            }
        }

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
