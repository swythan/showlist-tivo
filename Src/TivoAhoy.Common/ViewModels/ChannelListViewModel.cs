using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;

namespace TivoAhoy.Common.ViewModels
{
    public class ChannelListViewModel : Screen
    {
        private readonly IProgressService progressService;
        private readonly INavigationService navigationService;
        private readonly IScheduledRecordingsService scheduledRecordingsService;
        private readonly ITivoConnectionService connectionService;
        private readonly Func<OfferViewModel> offerViewModelFactory;

        private List<Channel> channels;
        private DateTime startTime;

        private IList shows;

        public ChannelListViewModel(
            IProgressService progressService,
            INavigationService navigationService,
            IScheduledRecordingsService scheduledRecordingsService,
            ITivoConnectionService connectionService,
            Func<OfferViewModel> offerViewModelFactory)
        {
            this.progressService = progressService;
            this.navigationService = navigationService;
            this.scheduledRecordingsService = scheduledRecordingsService;
            this.connectionService = connectionService;
            this.offerViewModelFactory = offerViewModelFactory;
            this.StartTime = DateTime.Now;

            connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
        }

        private void OnConnectionServicePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected")
            {
                NotifyOfPropertyChange(() => this.CanRefreshShows);

                if (this.IsActive)
                {
                    this.RefreshShows();
                }
            }
        }

        public ChannelListViewModel()
        {
            if (Execute.InDesignMode)
                LoadDesignData();
        }

        private void LoadDesignData()
        {
            this.Shows = new List<OfferViewModel>()
            {
                OfferViewModel.CreateDesignTime(
                    new Channel()
                    {
                            ChannelNumber = 101,
                            CallSign = "BBC 1",
                            LogoIndex = 65736
                    },
                    new Offer()
                    {
                        Title = "Antiques Roadshow",
                        StartTime = DateTime.Parse("17:00"),
                        DurationSeconds = 1800
                    }),      
                OfferViewModel.CreateDesignTime(
                    new Channel()
                    {
                            ChannelNumber = 102,
                            CallSign = "BBC 2",
                            LogoIndex = 65738
                    },
                    new Offer()
                    {
                        Title = "Charlie Brooker's Weekly Wipe",
                        StartTime = DateTime.Parse("17:15"),
                        DurationSeconds = 3600
                    }), 
            };
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshShows);
            NotifyOfPropertyChange(() => this.ShowSettingsPrompt);

            if (this.Shows == null ||
                this.Shows.Count == 0)
            {
                this.RefreshShows();
            }
        }

        public List<DateTime> Dates
        {
            get
            {
                var today = DateTime.Now.Date;

                return Enumerable.Range(-14, 28)
                    .Select(x => (today + TimeSpan.FromDays(x)).Date)
                    .ToList();
            }
        }

        public DateTime SelectedDate
        {
            get
            {
                return this.startTime.Date;
            }

            set
            {
                this.StartTime = value.Date + this.StartTime.TimeOfDay;
            }
        }

        public DateTime StartTime
        {
            get { return this.startTime; }
            set
            {
                if (this.startTime == value)
                {
                    return;
                }

                this.startTime = DateTime.SpecifyKind(value, DateTimeKind.Local);
                if (this.startTime.Second == 0)
                {
                    this.startTime = this.startTime + TimeSpan.FromSeconds(1);
                }

                this.NotifyOfPropertyChange(() => this.StartTime);
                this.NotifyOfPropertyChange(() => this.SelectedDate);
            }
        }

        public IList Shows
        {
            get { return this.shows; }
            private set
            {
                this.shows = value;
                this.NotifyOfPropertyChange(() => this.Shows);
            }
        }

        public bool CanRefreshShows
        {
            get
            {
                return this.connectionService.IsConnected;
            }
        }

        public bool ShowSettingsPrompt
        {
            get
            {
                return !this.connectionService.SettingsAppearValid;
            }
        }

        public async void RefreshShows()
        {
            if (this.CanRefreshShows)
            {
                await FetchChannels();
            }
        }

        private async Task FetchChannels()
        {
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (this.progressService.Show())
                {
                    if (this.channels == null)
                    {
                        this.channels = await connection.GetChannelsAsync();
                    }

                    this.Shows = this.channels
                        .Select(x => CreateOfferViewModel(x, this.StartTime))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
        }

        private OfferViewModel CreateOfferViewModel(Channel channel, DateTime time)
        {
            var model = this.offerViewModelFactory();
            model.Initialise(channel, time);

            return model;
        }

        public void DisplayOfferDetails(OfferViewModel offer)
        {
            if (offer == null ||
                offer.Offer == null ||
                offer.Offer.ContentId == null ||
                offer.Offer.OfferId == null)
            {
                return;
            }

            var scheduledRecording = this.scheduledRecordingsService.GetScheduledRecordingForOffer(offer.Offer.OfferId);

            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, offer.Offer.ContentId)
                .WithParam(x => x.ShowOfferID, offer.Offer.OfferId)
                .WithParam(x => x.ShowRecordingID, scheduledRecording == null ? null : scheduledRecording.RecordingId)
                .Navigate();
        }
    }
}
