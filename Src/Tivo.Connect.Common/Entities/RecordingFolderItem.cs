using System.ComponentModel;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class RecordingFolderItem : INotifyPropertyChanged
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

        [JsonProperty("objectIdAndType")]
        public long ObjectId { get; set; }

        [JsonProperty("collectionType")]
        public string ItemType { get; set; }

        public string Title { get; set; }
    }
}
