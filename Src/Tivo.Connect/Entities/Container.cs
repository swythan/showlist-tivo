using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class Container : RecordingFolderItem
    {
        public Container(dynamic jsonSource)
            : base((string)(jsonSource.recordingFolderItemId), (object) jsonSource)
        {

        }
    }
}
