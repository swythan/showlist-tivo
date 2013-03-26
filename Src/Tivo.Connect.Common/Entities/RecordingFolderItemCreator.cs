using System;
using Newtonsoft.Json.Linq;

namespace Tivo.Connect.Entities
{
    internal class RecordingFolderItemCreator : JsonCreationConverter<RecordingFolderItem>
    {
        protected override RecordingFolderItem Create(Type objectType, JObject jObject)
        {
            var childRecordingId = (string)jObject["childRecordingId"];
            var recordingFolderItemId = (string)jObject["recordingFolderItemId"];

            if (childRecordingId.Split('.')[1] == recordingFolderItemId.Split('.')[1])
            {
                return new IndividualShow();
            }
            else
            {
                return new Container();
            }
        }
    }
}
