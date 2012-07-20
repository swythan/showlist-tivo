using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class IndividualShow : RecordingFolderItem
    {
        public IndividualShow()
        { 
        }

        public string ContentId { get; set; }

        protected override void SetupFromRecordingFolderItemJson(IDictionary<string, object> jsonSource)
        {
            base.SetupFromRecordingFolderItemJson(jsonSource);

            this.Id = (string)jsonSource["childRecordingId"];
            this.ContentId = (string)jsonSource["contentId"];
        }
    }
}
