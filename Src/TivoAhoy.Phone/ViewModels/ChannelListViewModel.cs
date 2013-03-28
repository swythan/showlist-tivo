using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class ChannelListViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly INavigationService navigationService;
        private readonly ITivoConnectionService connectionService;

        private IList shows;

        public ChannelListViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            ITivoConnectionService connectionService,
            Func<OfferViewModel> offerViewModelFactory)
        {
            this.eventAggregator = eventAggregator;
            this.navigationService = navigationService;
            this.connectionService = connectionService;

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
                new OfferViewModel(
                    new Channel()
                    {
                            ChannelNumber = 101,
                            CallSign = "BBC 1",
                            LogoIndex = 65736
                    })
                    {
                        Offer = 
                            new Offer()
                            {
                                Title = "Antiques Roadshow"
                            }
                    },      
                new OfferViewModel(
                    new Channel()
                    {
                            ChannelNumber = 102,
                            CallSign = "BBC 2",
                            LogoIndex = 65738
                    }) 
                    {
                        Offer =
                            new Offer()
                            {
                                Title = "Charlie Brooker's Weekly Wipe"
                            }
                    }, 
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

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
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
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                var channels = await connection.GetChannelsAsync();
                this.Shows = new VirtualizedShowList(connection, channels, DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                OnOperationFinished();
            }
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

            this.navigationService
                .UriFor<ShowDetailsPageViewModel>()
                .WithParam(x => x.ShowContentID, offer.Offer.ContentId)
                .WithParam(x => x.ShowOfferID, offer.Offer.OfferId)
                .Navigate();
        }
    }
}
