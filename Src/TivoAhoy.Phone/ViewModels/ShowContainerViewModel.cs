using Caliburn.Micro;
using Tivo.Connect.Entities;
using Tivo.Connect;
using System.Windows;
using System;
using System.Reactive.Linq;

namespace TivoAhoy.Phone.ViewModels
{
    public class ShowContainerViewModel : RecordingFolderItemViewModel<Container>
    {
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settingsModel;

        private readonly Func<IndividualShowViewModel> showViewModelFactory;
        private readonly Func<ShowContainerViewModel> showContainerViewModelFactory;


        public ShowContainerViewModel(
            ISterlingInstance sterlingInstance,
            SettingsPageViewModel settingsModel,
            Func<IndividualShowViewModel> showViewModelFactory,
            Func<ShowContainerViewModel> showContainerViewModelFactory)
        {
            this.sterlingInstance = sterlingInstance;
            this.settingsModel = settingsModel;

            this.showViewModelFactory = showViewModelFactory;
            this.showContainerViewModelFactory = showContainerViewModelFactory;

            this.shows = new BindableCollection<IRecordingFolderItemViewModel>();
        }

        public override bool IsSingleShow
        {
            get { return false; }
        }

        private BindableCollection<IRecordingFolderItemViewModel> shows;
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
                return string.Format("{0} shows", this.Source.FolderItemCount);
            }
        }

        public void GetChildShows()
        {
            this.Shows.Clear();

            var connection = new TivoConnection(sterlingInstance.Database);

            connection.Connect(this.settingsModel.TivoIPAddress, this.settingsModel.MediaAccessKey)
                .SelectMany(_ => connection.GetMyShowsList(this.Source))
                .ObserveOnDispatcher()
                .Subscribe(
                    show =>
                    {
                        this.Shows.Add(CreateItemViewModel(show));
                        NotifyOfPropertyChange(() => this.ContentInfo);
                    },
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                        connection.Dispose();
                    },
                    () => connection.Dispose());
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
