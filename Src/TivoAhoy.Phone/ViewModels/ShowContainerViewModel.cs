using System;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowContainerViewModel : RecordingFolderItemViewModel<Container>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private readonly Func<IndividualShowViewModel> showViewModelFactory;
        private readonly Func<ShowContainerViewModel> showContainerViewModelFactory;

        private BindableCollection<IRecordingFolderItemViewModel> shows;

        public ShowContainerViewModel(
            IEventAggregator eventAggregator,
            ISterlingInstance sterlingInstance,
            SettingsPageViewModel settingsModel,
            Func<IndividualShowViewModel> showViewModelFactory,
            Func<ShowContainerViewModel> showContainerViewModelFactory)
        {
            this.eventAggregator = eventAggregator;
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;

            this.showViewModelFactory = showViewModelFactory;
            this.showContainerViewModelFactory = showContainerViewModelFactory;

            this.shows = new BindableCollection<IRecordingFolderItemViewModel>();
        }

        private void OnOperationStarted()
        {
            this.eventAggregator.Publish(new TivoOperationStarted());
        }

        private void OnOperationFinished()
        {
            this.eventAggregator.Publish(new TivoOperationFinished());
        }

        public override bool IsSingleShow
        {
            get { return false; }
        }

        public BindableCollection<IRecordingFolderItemViewModel> Shows
        {
            get { return shows; }
            set
            {
                if (this.shows == value)
                    return;

                this.shows = value;
                NotifyOfPropertyChange(() => this.Shows);
            }
        }
                        
        public string ContentInfo
        {
            get
            {
                if (string.Equals(this.Source.FolderType, "series", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format("{0} episodes", this.Source.FolderItemCount);
                }
                else
                {
                    return string.Format("{0} shows", this.Source.FolderItemCount);              
                }
            }
        }

        public async void GetChildShows()
        {
            this.Shows.Clear();

            var connection = new TivoConnection(sterlingInstance.Database);

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.settingsModel.Username, this.settingsModel.Password);

                var progress = new Progress<RecordingFolderItem>(
                    show =>
                    {
                        this.Shows.Add(CreateItemViewModel(show));
                        NotifyOfPropertyChange(() => this.ContentInfo);
                    });

                await connection.GetMyShowsList(this.Source, progress);
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
    }
}
