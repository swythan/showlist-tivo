using System;
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

        public BindableCollection<IRecordingFolderItemViewModel> MyShows { get; private set; }

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

        public void RefreshShows()
        {
            FetchShows(null);
        }

        private async void FetchShows(Container parent)
        {
            this.MyShows.Clear();

            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.settingsModel.Username, this.settingsModel.Password);

                var progress = new Progress<RecordingFolderItem>(show => this.MyShows.Add(CreateItemViewModel(show)));

                await connection.GetMyShowsList(parent, progress);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                connection.Dispose();
                OnOperationFinished();
            }
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
