using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;

namespace TivoTest
{
    [Export(typeof(MainViewModel))]
    public partial class MainViewModel : PropertyChangedBase
    {
        private TivoConnection connection;
        private ISterlingInstance sterlingInstance;

        private BindableCollection<RecordingFolderItem> shows;

        [ImportingConstructor]
        public MainViewModel(ISterlingInstance sterlingInstance)
        {
            this.sterlingInstance = sterlingInstance;

            this.shows = new BindableCollection<RecordingFolderItem>();
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
            var localConnection = new TivoConnection(this.sterlingInstance.Database);
            try
            {
                await localConnection.Connect(IPAddress.Parse("192.168.0.100"), "9837127953");

                this.connection = localConnection;

                NotifyOfPropertyChange(() => CanFetchMyShowsList);
                NotifyOfPropertyChange(() => CanGetWhatsOn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                localConnection.Dispose();
            }

        }

        public async void ConnectAwayMode()
        {
            var localConnection = new TivoConnection(this.sterlingInstance.Database);
            try
            {
                await localConnection.ConnectAway(@"james.chaldecott@virginmedia.com", @"lambBh00na");

                this.connection = localConnection;

                NotifyOfPropertyChange(() => CanFetchMyShowsList);
                NotifyOfPropertyChange(() => CanGetWhatsOn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                localConnection.Dispose();
            }

        }

        public bool CanFetchMyShowsList
        {
            get
            {
                return this.connection != null;
            }
        }

        public bool CanGetWhatsOn
        {
            get
            {
                return this.connection != null;
            }
        }

        public async void GetWhatsOn()
        {
            try
            {
                var result = await connection.GetWhatsOn();

                Console.WriteLine(result);
                
                MessageBox.Show("What's On Finished!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void FetchMyShowsList()
        {
            try
            {
                shows.Clear();

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
                    await connection.PlayShow(show.Id);
                }
            }
        }
    }
}
