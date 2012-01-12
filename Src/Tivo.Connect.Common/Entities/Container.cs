using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class Container : RecordingFolderItem
    {
        public Container()
        {
        }

        protected override void SetupFromRecordingFolderItemJson(IDictionary<string, object> jsonSource)
        {
            base.SetupFromRecordingFolderItemJson(jsonSource);

            this.Id = (string)jsonSource["recordingFolderItemId"];

            if (jsonSource.ContainsKey("folderItemCount"))
            {
                this.FolderItemCount = (int)jsonSource["folderItemCount"];
            }
        }

        public int FolderItemCount { get; set; }
    }
}
