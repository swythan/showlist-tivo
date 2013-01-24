using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JsonFx.Json;

namespace Tivo.Connect.Entities
{
    public class GridRow : INotifyPropertyChanged
    {
        private IDictionary<string, object> jsonSource;

        public GridRow(IDictionary<string, object> jsonSource)
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

        public Channel Channel { get; set; }
        public List<Offer> Offers { get; set; }

        private void SetupFromJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            if (this.jsonSource.ContainsKey("channel"))
            {
                this.Channel = new Channel((IDictionary<string, object>)jsonSource["channel"]);
            }

            if (this.jsonSource.ContainsKey("offer"))
            {
                this.Offers = ((IEnumerable<IDictionary<string, object>>)jsonSource["offer"])
                    .Select(x => new Offer(x))
                    .ToList();
            }
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
