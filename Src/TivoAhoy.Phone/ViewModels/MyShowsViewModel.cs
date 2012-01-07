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
using Caliburn.Micro;
using System.Collections.Generic;
using Tivo.Connect.Entities;
using Tivo.Connect;

namespace TivoAhoy.Phone.ViewModels
{
    public class MyShowsViewModel : Screen
    {
        private readonly SettingsPageViewModel settingsModel;

        public MyShowsViewModel(SettingsPageViewModel settingsModel)
        {
            this.settingsModel = settingsModel;

            this.MyShows = new BindableCollection<IRecordingFolderItemViewModel>();

        }

        public BindableCollection<IRecordingFolderItemViewModel> MyShows { get; private set; }

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
            FetchShows(null);
        }

        private void FetchShows(Container parent)
        {
            IEnumerable<RecordingFolderItem> shows;

            using (var connection = new TivoConnection())
            {
                try
                {
                    connection.Connect(this.settingsModel.TivoIPAddress, this.settingsModel.MediaAccessKey);

                    if (parent == null)
                    {
                        shows = connection.GetMyShowsList();
                    }
                    else
                    {
                        shows = connection.GetFolderShowsList(parent);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                    return;
                }
            }

            PopulateShowList(shows);
        }

        private void PopulateShowList(IEnumerable<RecordingFolderItem> shows)
        {
            this.MyShows.Clear();

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

        public void ActivateItem(object item)
        {
            var showContainer = item as ShowContainerViewModel;

            if (showContainer != null)
            {
                FetchShows(showContainer.Source);
            }
        }
    }
}
