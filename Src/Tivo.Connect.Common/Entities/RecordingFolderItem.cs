using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonFx.Json;

namespace Tivo.Connect.Entities
{
    public class RecordingFolderItem
    {
        public static RecordingFolderItem Create(IDictionary<string, object> jsonSource)
        {
            if (((string)jsonSource["childRecordingId"]).Split('.')[1] == ((string)jsonSource["recordingFolderItemId"]).Split('.')[1])
            {
                return new IndividualShow(jsonSource);
            }

            return new Container(jsonSource);
        }

        private readonly IDictionary<string, object> jsonSource;

        public RecordingFolderItem(string id, IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            this.Id = id;
            this.ItemType = jsonSource["collectionType"] as string;
            this.Title = jsonSource["title"] as string;
        }

        public string Id { get; private set; }
        public string ItemType { get; private set; }
        public string Title { get; private set; }

        public string JsonText
        {
            get
            {
                var writer = new JsonWriter();
                writer.Settings.PrettyPrint = true;

                return writer.Write(this.jsonSource);
            }
        }
    }
}
