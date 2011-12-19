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

namespace TivoTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private TivoConnection connection;
        private IEnumerable<object> shows;

        private void NotifyPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public IEnumerable<object> Shows
        {
            get { return this.shows; }
            set
            {
                if (value == this.shows)
                    return;

                this.shows = value;
                NotifyPropertyChanged("Shows");
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.connection = new TivoConnection();
            try
            {
                connection.Connect("192.168.0.16", "9837127953");
                MessageBox.Show(this, "Connection succeeeded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("Connection Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MyShowsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var shows = connection.GetMyShowsList();
                this.Shows = shows;
                MessageBox.Show(this, "Request succeeeded!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("Request Failed\n{0}", ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}
