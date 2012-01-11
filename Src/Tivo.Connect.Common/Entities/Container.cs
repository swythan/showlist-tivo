using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class Container : RecordingFolderItem
    {
        public Container(IDictionary<string, object> jsonSource)
            : base((string)jsonSource["recordingFolderItemId"], jsonSource)
        {

        }
    }
}
