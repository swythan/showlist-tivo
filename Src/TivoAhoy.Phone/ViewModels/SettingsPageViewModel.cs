using System;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;

namespace TivoAhoy.Phone.ViewModels
{
    public class SettingsPageModelStorage : StorageHandler<SettingsPageViewModel>
    {
        public override void Configure()
        {
            this.Property(x => x.TivoIPAddress)
                .InAppSettings();

            this.Property(x => x.MediaAccessKey)
                .InAppSettings();
        }
    }

    public class SettingsPageViewModel : PropertyChangedBase
    {
        private string tivoIPAddress;
        private string mediaAccessKey;

        public SettingsPageViewModel()
        {
        }

        public string TivoIPAddress
        {
            get { return this.tivoIPAddress; }
            set
            {
                if (this.tivoIPAddress == value)
                    return;

                this.tivoIPAddress = value;
                NotifyOfPropertyChange(() => this.TivoIPAddress);
            }
        }

        public string MediaAccessKey
        {
            get { return this.mediaAccessKey; }
            set
            {
                if (this.mediaAccessKey == value)
                    return;

                this.mediaAccessKey = value;
                NotifyOfPropertyChange(() => this.MediaAccessKey);
            }
        }

        public IPAddress ParsedIPAddress
        {
            get
            {
                IPAddress ipAddress;
                if (IPAddress.TryParse(this.TivoIPAddress, out ipAddress))
                {
                    return ipAddress;
                }

                return IPAddress.None;
            }
        }

        public void TestConnection()
        {
            var connection = new TivoConnection();

            IPAddress ipAddress;
            if (!IPAddress.TryParse(this.TivoIPAddress, out ipAddress))
            {
                MessageBox.Show("Please enter a valid IP address");
                return;
            }

            connection.Connect(ipAddress, this.MediaAccessKey)
                .ObserveOnDispatcher()
                .Subscribe(
                    _ => MessageBox.Show("Connection Succeeded!"),
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
                        connection.Dispose();
                    },
                    () => connection.Dispose());

        }
    }
}
