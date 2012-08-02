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
using Caliburn.Micro;

namespace TivoAhoy.Phone.ViewModels
{
    public class IndividualShowViewModel : RecordingFolderItemViewModel<IndividualShow>
    {
        private readonly INavigationService navigationService;

        public IndividualShowViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public override bool IsSingleShow
        {
            get { return true; }
        }

        public void DisplayShowDetails()
        {
            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, this.Source.ContentId)
                .WithParam(x => x.ShowRecordingID, this.Source.Id)
                .Navigate();
        }
    }
}
