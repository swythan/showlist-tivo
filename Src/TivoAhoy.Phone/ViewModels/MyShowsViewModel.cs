using System;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class MyShowsViewModel : Screen
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private readonly Func<IndividualShowViewModel> showViewModelFactory;
        private readonly Func<ShowContainerViewModel> showContainerViewModelFactory;

        public MyShowsViewModel(
            IEventAggregator eventAggregator,
            ISterlingInstance sterlingInstance, 
            SettingsPageViewModel settingsModel, 
            Func<IndividualShowViewModel> showViewModelFactory,
            Func<ShowContainerViewModel> showContainerViewModelFactory)
        {
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;
            this.eventAggregator = eventAggregator;

            this.showViewModelFactory = showViewModelFactory;
            this.showContainerViewModelFactory = showContainerViewModelFactory;

            this.MyShows = new BindableCollection<IRecordingFolderItemViewModel>();
        }

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());            
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
            this.MyShows.Clear();

            var connection = new TivoConnection(sterlingInstance.Database);

            var ipAddress = IPAddress.Parse(this.settingsModel.TivoIPAddress);

            OnOperationStarted();

            connection.Connect(ipAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.GetMyShowsList(parent))
                .Finally(
                    () => 
                    {
                        connection.Dispose();
                        OnOperationFinished();
                    }) 
                .ObserveOnDispatcher()
                .Subscribe(
                    show => this.MyShows.Add(CreateItemViewModel(show)),
                    ex => MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message)));
        }

        private IRecordingFolderItemViewModel CreateItemViewModel(RecordingFolderItem recordingFolderItem)
        {
            var showContainer = recordingFolderItem as Container;
            if (showContainer != null)
            {
                var result = this.showContainerViewModelFactory();
                result.Source = showContainer;

                return result;
            }

            var show = recordingFolderItem as IndividualShow;
            if (show != null)
            {
                var result = this.showViewModelFactory();
                result.Source = show;

                return result; 
            }

            return null;
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
