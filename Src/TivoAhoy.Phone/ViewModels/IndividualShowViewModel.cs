using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone.ViewModels
{
    public class IndividualShowViewModel : RecordingFolderItemViewModel<IndividualShow>
    {
        public override bool IsSingleShow
        {
            get { return true; }
        }
    }
}
