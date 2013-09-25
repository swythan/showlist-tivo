//-----------------------------------------------------------------------
// <copyright file="IndividualShowViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using Tivo.Connect.Entities;

namespace TivoAhoy.Common.ViewModels
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
                    StartTime = DateTime.Parse("2013/05/01 21:00"),
                    RecordingForChildRecordingId =
                    new Recording
                    {
                        EpisodeNumbers = new List<int> { 5 },
                        SeasonNumber = 2
                    }
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

        public bool HasEpisodeNumbers
        {
            get
            {
                return
                    this.Source != null &&
                    this.Source.RecordingForChildRecordingId != null &&
                    this.Source.RecordingForChildRecordingId.EpisodeNumber != null &&
                    this.Source.RecordingForChildRecordingId.SeasonNumber != null;
            }
        }

        public void DisplayShowDetails()
        {
            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, this.Source.ContentId)
                .WithParam(x => x.ShowRecordingID, this.Source.Id)
                .WithParam(x => x.ShowOfferID, this.Source.RecordingForChildRecordingId.OfferId)
                .Navigate();
        }
    }
}
