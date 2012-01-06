using System.Collections.ObjectModel;
using Caliburn.Micro;
using System;
using Tivo.Connect;
using System.Windows;
using System.Collections.Generic;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone
{
    public class MainPageViewModel : Screen
    {
        private readonly INavigationService navigationService;
        private readonly SettingsPageViewModel settingsModel;

        public MainPageViewModel(INavigationService navigationService, SettingsPageViewModel settingsModel)
        {
            this.navigationService = navigationService;
            this.settingsModel = settingsModel;

            this.MyShows = new BindableCollection<IRecordingFolderItemViewModel>();
        }

        public ObservableCollection<IRecordingFolderItemViewModel> MyShows { get; private set; }

        public void ShowSettings()
        {
            navigationService.UriFor<SettingsPageViewModel>().Navigate();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            NotifyOfPropertyChange(() => this.CanRefreshShows);
        }

        public bool CanRefreshShows
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.settingsModel.TivoIPAddress))
                    return false;

                if (string.IsNullOrWhiteSpace(this.settingsModel.MediaAccessKey))
                    return false;

                return true;
            }
        }

        public void RefreshShows()
        {
            this.MyShows.Clear();

            IEnumerable<RecordingFolderItem> shows;

            using (var connection = new TivoConnection())
            {
                try
                {
                    connection.Connect(this.settingsModel.TivoIPAddress, this.settingsModel.MediaAccessKey);

                    shows = connection.GetMyShowsList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                    return;
                }

                foreach (var recordingFolderItem in shows)
                {
                    var showContainer = recordingFolderItem as Container;
                    if (showContainer != null)
                    {
                        this.MyShows.Add(new ShowContainerViewModel() { Source = showContainer });
                    }

                    var show = recordingFolderItem as IndividualShow;
                    if (show != null)
                    {
                        this.MyShows.Add(new IndividualShowViewModel() { Source = show });
                    }
                }
            }
        }
    }
}