using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace Tivo.Connect.Entities
{
    public class IndividualShow : RecordingFolderItem
    {
        public IndividualShow()
        { 
        }

        public string ContentId { get; set; }
        public DateTime StartTime { get; set; }

        protected override void SetupFromRecordingFolderItemJson(IDictionary<string, object> jsonSource)
        {
            base.SetupFromRecordingFolderItemJson(jsonSource);

            this.Id = (string)jsonSource["childRecordingId"];
            this.ContentId = (string)jsonSource["contentId"];

            if (jsonSource.ContainsKey("startTime"))
            {
                var startTimeString = (string)jsonSource["startTime"];
                DateTime startTime;
                if (DateTime.TryParse(startTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out startTime))
                {
                    this.StartTime = startTime;
                }
                else
                {
                    Debug.WriteLine(
                        "Failed to parse startTime: {0}",
                        jsonSource["startTime"]);
                }
            }
        }

    }
}
