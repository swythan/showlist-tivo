﻿using System;
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
            RecordingFolderItem result = null;

            if (((string)jsonSource["childRecordingId"]).Split('.')[1] == ((string)jsonSource["recordingFolderItemId"]).Split('.')[1])
            {
                result = new IndividualShow();
            }
            else
            { 
                result =  new Container();
            }

            result.SetupFromRecordingFolderItemJson(jsonSource);

            return result;
        }

        private IDictionary<string, object> jsonSource;

        public RecordingFolderItem()
        {

        }

        protected virtual void SetupFromRecordingFolderItemJson(IDictionary<string, object> jsonSource)
        {
            this.jsonSource = jsonSource;

            this.ObjectId = long.Parse(jsonSource["objectIdAndType"] as string);
            this.ItemType = jsonSource["collectionType"] as string;
            this.Title = jsonSource["title"] as string;
        }

        public long ObjectId { get; set; }
        public string Id { get; set; }
        public string ItemType { get; set; }
        public string Title { get; set; }

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