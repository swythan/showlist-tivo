using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tivo.Connect.Entities
{
    public class IndividualShow : RecordingFolderItem
    {
        public IndividualShow(dynamic jsonSource)
            : base((string)jsonSource.childRecordingId, (object)jsonSource)
        {

        }
    }
}
