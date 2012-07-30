using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tivo.Connect;
using System.ComponentModel;
using Caliburn.Micro;
using System.ComponentModel.Composition;
using Tivo.Connect.Entities;
using System.Reactive.Linq;
using System.Reactive.Threading;
using System.Net;

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

        public void Connect()
        {
            var localConnection = new TivoConnection(this.sterlingInstance.Database);
            try
            {
                localConnection.Connect(IPAddress.Parse("192.168.0.100"), "9837127953")
                    .Subscribe(
                        _ => this.connection = localConnection,
                        ex =>
                        {
                            MessageBox.Show(string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            localConnection.Dispose();
                        },
                        () => NotifyOfPropertyChange(() => CanFetchMyShowsList));
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
                return this.connection != null;
            }
        }

        public void FetchMyShowsList()
        {
            try
            {
                shows.Clear();

                connection.GetMyShowsList(null)
                    .Subscribe(item => this.Shows.Add(item), () => MessageBox.Show("Shows updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ActivateItem(RecordingFolderItem item)
        {
            var folder = item as Tivo.Connect.Entities.Container;

            if (folder != null)
            {
                try
                {
                    shows.Clear();

                    connection.GetMyShowsList(folder)
                        .Subscribe(child => this.Shows.Add(child), () => MessageBox.Show("Shows updated!", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
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
                    connection.PlayShow(show.Id);
                }
            }
        }
    }
}
