using System;
using Newtonsoft.Json.Linq;

namespace Tivo.Connect.Entities
{
    internal class RecordingFolderItemCreator : JsonCreationConverter<RecordingFolderItem>
    {
        protected override RecordingFolderItem Create(Type objectType, JObject jObject)
        {
            int? folderItemCount = (int?)jObject["folderItemCount"];

            if (folderItemCount != null &
                folderItemCount > 0)
            {
                return new Container();
            }
            else
            {
                return new IndividualShow();
            }
        }
    }
}
