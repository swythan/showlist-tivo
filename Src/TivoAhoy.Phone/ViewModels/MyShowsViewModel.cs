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
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private readonly Func<LazyRecordingFolderItemViewModel> showModelFactory;

        private IEnumerable<LazyRecordingFolderItemViewModel> myShows;

        public MyShowsViewModel(
            IProgressService progressService,
            ITivoConnectionService connectionService,
            Func<LazyRecordingFolderItemViewModel> showModelFactory)
        {
            this.connectionService = connectionService;
            this.progressService = progressService;

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
            try
            {
                var connection = await this.connectionService.GetConnectionAsync();

                using (this.progressService.Show())
                {
                    var ids = await connection.GetRecordingFolderItemIds(parent != null ? parent.Id : null);
                    this.MyShows = ids
                        .Select(CreateShowViewModel)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
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
