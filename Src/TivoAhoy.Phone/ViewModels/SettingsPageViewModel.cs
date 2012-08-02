using System;
using System.Net;
using System.Reactive.Linq;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using TivoAhoy.Phone.Events;

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
        private bool isTestInProgress;

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
                NotifyOfPropertyChange(() => this.CanTestConnection);
                NotifyOfPropertyChange(() => this.SettingsAppearValid);
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
                NotifyOfPropertyChange(() => this.CanTestConnection);
                NotifyOfPropertyChange(() => this.SettingsAppearValid);
            }
        }

        public bool IsTestInProgress
        {
            get { return this.isTestInProgress; }
        }

        private void OnOperationStarted()
        {
            this.isTestInProgress = true;
            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanTestConnection);
        }

        private void OnOperationFinished()
        {
            this.isTestInProgress = false;
            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanTestConnection);
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
        
        public bool CanTestConnection
        {
            get
            {
                return this.SettingsAppearValid && !this.IsTestInProgress;
            }
        }

        public bool SettingsAppearValid
        {
            get
            {
                if (this.MediaAccessKey == null ||
                    this.MediaAccessKey.Length != 10)
                    return false;

                long makAsLong;
                if (!long.TryParse(this.MediaAccessKey, out makAsLong))
                    return false;

                IPAddress ipAddress;
                if (!IPAddress.TryParse(this.TivoIPAddress, out ipAddress))
                    return false;

                return true;
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

            OnOperationStarted();
            connection.Connect(ipAddress, this.MediaAccessKey)
                .Finally(
                    () => 
                    {
                        connection.Dispose();
                        OnOperationFinished();
                    }) 
                .ObserveOnDispatcher()
                .Subscribe(
                    _ => MessageBox.Show("Connection Succeeded!"),
                    ex => MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message)));
        }
    }
}
