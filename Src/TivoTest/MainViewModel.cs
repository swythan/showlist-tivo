using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoTest
{
    [Export(typeof(MainViewModel))]
    public partial class MainViewModel : PropertyChangedBase
    {
        private ITivoConnectionService tivoConnectionService;

        private BindableCollection<RecordingFolderItem> shows;

        [ImportingConstructor]
        public MainViewModel(
            ITivoConnectionService tivoConnectionService,
            WhatsOnViewModel whatsOnModel,
            ShowGridViewModel showGridModel)
        {
            this.tivoConnectionService = tivoConnectionService;
            this.WhatsOn = whatsOnModel;
            this.ShowGrid = showGridModel;

            this.tivoConnectionService.PropertyChanged += OnTivoConnectionServicePropertyChanged;
            this.shows = new BindableCollection<RecordingFolderItem>();
        }

        public WhatsOnViewModel WhatsOn { get; private set; }
        public ShowGridViewModel ShowGrid { get; private set; }

        void OnTivoConnectionServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => CanFetchMyShowsList);
        }

        public BindableCollection<RecordingFolderItem> Shows
        {
            get { return this.shows; }
            set
            {
                if (value == this.shows)
                    return;

                this.shows = value;
                NotifyOfPropertyChange(() => this.Shows);
            }
        }

        public async void Connect()
        {
            this.tivoConnectionService.IsAwayModeEnabled = false;

            await TestConnection();
        }

        public async void ConnectAwayMode()
        {
            this.tivoConnectionService.IsAwayModeEnabled = true;
            
            await TestConnection();
        }

        private async Task TestConnection()
        {
            try
            {
                await this.tivoConnectionService.GetConnectionAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanFetchMyShowsList
        {
            get
            {
                return this.tivoConnectionService.IsConnected;
            }
        }
        public async void FetchMyShowsList()
        {
            try
            {
                shows.Clear();

                var connection = await this.tivoConnectionService.GetConnectionAsync();

                var progress = new Progress<RecordingFolderItem>(item => this.Shows.Add(item));

                await connection.GetMyShowsList(null, progress);
                
                MessageBox.Show("Shows updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void ActivateItem(RecordingFolderItem item)
        {
            var folder = item as Tivo.Connect.Entities.Container;

            if (folder != null)
            {
                try
                {
                    shows.Clear();

                    var connection = await this.tivoConnectionService.GetConnectionAsync();

                    var progress = new Progress<RecordingFolderItem>(child => this.Shows.Add(child));
                    await connection.GetMyShowsList(folder, progress);

                    MessageBox.Show("Shows updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                var show = item as Tivo.Connect.Entities.IndividualShow;

                if (show != null)
                {
                    var connection = await this.tivoConnectionService.GetConnectionAsync();

                    await connection.PlayShow(show.Id);
                }
            }
        }
    }
}
