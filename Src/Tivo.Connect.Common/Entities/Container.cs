//-----------------------------------------------------------------------
// <copyright file="Container.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tivo.Connect.Entities
{
    public class Container : RecordingFolderItem
    {
        public Container()
        {
            this.FolderItemCount = 1;
            this.FolderType = string.Empty;
        }

        [JsonProperty("recordingFolderItemId")]
        public string Id { get; set; }

        public int FolderItemCount { get; set; }
        public string FolderType { get; set; }
    }
}
