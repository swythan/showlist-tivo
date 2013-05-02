using System;
using System.Linq;
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

        public IndividualShowViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Source = 
                new IndividualShow()
                {
                    Title = "The Walking Dead",
                    StartTime = DateTime.Parse("2013/05/01 21:00")
                };
        }

        public override bool IsSingleShow
        {
            get { return true; }
        }

        public override bool IsSuggestion
        {
            get
            {
                IndividualShow source = this.Source;
                if (source == null)
                    return false;

                Recording recording = source.RecordingForChildRecordingId;
                if (recording == null)
                    return false;

                var subscriptions = recording.SubscriptionIdentifier;

                if (subscriptions == null)
                    return false;

                if (subscriptions.Any(x => x.SubscriptionType != "suggestions"))
                    return false;

                return true;
            }
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
