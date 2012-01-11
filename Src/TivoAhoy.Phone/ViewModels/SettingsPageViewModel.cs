using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using System.Net.Sockets;
using Tivo.Connect;
using System.Reactive.Linq;

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

        public void TestConnection()
        {
            var connection = new TivoConnection();

            connection.Connect(this.TivoIPAddress, this.MediaAccessKey)
                .ObserveOnDispatcher()
                .Subscribe(
                    _ => MessageBox.Show("Connection Succeeded!"),
                    ex =>
                    {
                        MessageBox.Show(string.Format("Connection Failed! :-(\n{0}", ex));
                        connection.Dispose();
                    },
                    () => connection.Dispose());

        }
    }
}
