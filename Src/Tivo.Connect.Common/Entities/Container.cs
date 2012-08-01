using System.Collections.Generic;

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
            else
            {
                this.FolderItemCount = 1;
            }

            if (jsonSource.ContainsKey("folderType"))
            {
                this.FolderType = (string)jsonSource["folderType"];
            }
            else
            {
                this.FolderType = string.Empty;
            }
        }

        public int FolderItemCount { get; set; }
        public string FolderType { get; set; }
    }
}
