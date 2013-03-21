using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Microsoft;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class ChannelListViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;
        private IList<Channel> channels;
        private IList shows;

        public ChannelListViewModel(
            IEventAggregator eventAggregator,
            ISterlingInstance sterlingInstance,
            SettingsPageViewModel settingsModel)
        {
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
            this.eventAggregator = eventAggregator;
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

                var newChannels = new BindableCollection<Channel>();
                this.Channels = newChannels;

                this.Shows = new VirtualizedShowList(connection, newChannels, DateTime.Now);

                int offset = 0;
                int pageCount = 50;

                bool channelsAdded;
                do
                {
                    var extraChannels = await connection.GetChannelsAsync(pageCount, offset);

                    newChannels.AddRange(extraChannels);

                    channelsAdded = extraChannels.Count > 0;
                    channelsAdded = false;
                    offset += extraChannels.Count;
                } while (channelsAdded);

                this.Shows = new VirtualizedShowList(connection, newChannels, DateTime.Now);
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
    }
}
