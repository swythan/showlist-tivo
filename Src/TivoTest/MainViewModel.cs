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

namespace TivoTest
{
    [Export(typeof(MainViewModel))]
    public partial class MainViewModel : PropertyChangedBase
    {
        private TivoConnection connection;
        private IEnumerable<RecordingFolderItem> shows;

        public MainViewModel()
        {
        }

        public IEnumerable<RecordingFolderItem> Shows
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
            this.connection = new TivoConnection();
            try
            {
                connection.Connect("192.168.0.7", "9837127953");
                //MessageBox.Show("Connection succeeeded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.connection = null;
            }

            NotifyOfPropertyChange(() => CanFetchMyShowsList);
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
                var shows = connection.GetMyShowsList().Take(10).ToList();
                this.Shows = shows;
                // MessageBox.Show("Request succeeeded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    var shows = connection.GetFolderShowsList(folder);
                    this.Shows = shows;
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
                    connection.PlayShow(show);
                }
            }
        }
    }
}
