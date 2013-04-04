using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Microsoft;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class MyShowsViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;

        private readonly Func<LazyRecordingFolderItemViewModel> showModelFactory;

        private IEnumerable<LazyRecordingFolderItemViewModel> myShows;

        public MyShowsViewModel(
            IEventAggregator eventAggregator,
            ITivoConnectionService connectionService,
            Func<LazyRecordingFolderItemViewModel> showModelFactory)
        {
            this.connectionService = connectionService;
            this.eventAggregator = eventAggregator;

            this.showModelFactory = showModelFactory;

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

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshShows);
            NotifyOfPropertyChange(() => this.ShowSettingsPrompt);

            if (this.MyShows == null ||
                !this.MyShows.Any())
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

        public IEnumerable<LazyRecordingFolderItemViewModel> MyShows 
        { 
            get
            {
                return this.myShows;
            }

            private set
            {
                this.myShows = value;
                NotifyOfPropertyChange(() => this.MyShows);
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

        public void RefreshShows()
        {
            if (this.CanRefreshShows)
            {
                FetchShows(null);
            }
        }

        private async void FetchShows(Container parent)
        {
            OnOperationStarted();

            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                var ids = await connection.GetRecordingFolderItemIds(parent != null ? parent.Id : null);
                this.MyShows = ids
                    .Select(CreateShowViewModel)
                    .ToList();
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

        private LazyRecordingFolderItemViewModel CreateShowViewModel(long itemId)
        {
            var model = showModelFactory();
            model.Initialise(itemId);

            return model;
        }
        
        public void ActivateItem(object item)
        {
            //var showContainer = item as ShowContainerViewModel;

            //if (showContainer != null)
            //{
            //    FetchShows(showContainer.Source);
            //}
        }
    }
}
