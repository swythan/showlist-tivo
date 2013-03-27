﻿using System;
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
        private readonly ISterlingInstance sterlingInstance;

        private readonly SettingsPageViewModel settingsModel;

        private IList<Channel> channels;
        private IList shows;

        public ChannelListViewModel(
            IEventAggregator eventAggregator,
            INavigationService navigationService,
            ISterlingInstance sterlingInstance,
            SettingsPageViewModel settingsModel,
            Func<OfferViewModel> offerViewModelFactory)
        {
            this.eventAggregator = eventAggregator;
            this.navigationService = navigationService;
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
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
        }

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
        }

        public IList<Channel> Channels
        {
            get { return this.channels; }
            private set
            {
                this.channels = value;
                this.NotifyOfPropertyChange(() => this.Channels);
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
                return this.settingsModel.SettingsAppearValid;
            }
        }

        public bool ShowSettingsPrompt
        {
            get
            {
                return !this.settingsModel.SettingsAppearValid;
            }
        }

        public async void RefreshShows()
        {
            await FetchChannels();
        }

        private async Task FetchChannels()
        {
            OnOperationStarted();

            var connection = new TivoConnection(sterlingInstance.Database);

            try
            {
                await connection.ConnectAway(this.settingsModel.Username, this.settingsModel.Password);

                this.Channels = await connection.GetChannelsAsync();
                this.Shows = new VirtualizedShowList(connection, this.Channels, DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                // connection.Dispose();
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
