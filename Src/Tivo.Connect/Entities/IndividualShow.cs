//-----------------------------------------------------------------------
// <copyright file="IndividualShow.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class IndividualShow : RecordingFolderItem
    {
        public IndividualShow()
        { 
        }

        [JsonProperty("childRecordingId")]
        public string Id { get; set; }

        public string ContentId { get; set; }
        public DateTime StartTime { get; set; }

        public Recording RecordingForChildRecordingId { get; set; }
    }
}
