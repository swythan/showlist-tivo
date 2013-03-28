using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect;
using TivoAhoy.Phone.Events;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone
{
    public class TivoConnectionService : PropertyChangedBase, ITivoConnectionService
    {
        private readonly IEventAggregator eventAggregator;
        private readonly ISterlingInstance sterlingInstance;
        private readonly SettingsPageViewModel settings;

        private bool isAwayModeEnabled = true;
        private bool isConnectionEnabled = false;

        private AsyncLazy<TivoConnection> lazyConnection;
        private bool isConnected = false;
        private string error;

        public TivoConnectionService(
            IEventAggregator eventAggregator,
            ISterlingInstance sterlingInstance, 
            SettingsPageViewModel settings)
        {
            this.eventAggregator = eventAggregator;
            this.sterlingInstance = sterlingInstance;
            this.settings = settings;

            this.settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        public bool IsConnectionEnabled
        {
            get
            {
                return this.isConnectionEnabled;
            }
            set
            {
                if (this.isConnectionEnabled == value)
                {
                    return;
                }

                this.isConnectionEnabled = value;
                NotifyOfPropertyChange(() => this.IsConnectionEnabled);

                ResetConnection();
            }
        }

        private void ResetConnection()
        {
            this.isConnected = false;
            this.lazyConnection = null;

            this.NotifyOfPropertyChange(() => this.IsConnected);

            AutoConnect();
        }

        private async Task AutoConnect()
        {
            if (this.SettingsAppearValid)
            {
                this.lazyConnection = new AsyncLazy<TivoConnection>(() => this.ConnectAsync());

                await TaskEx.Delay(TimeSpan.FromSeconds(2));

                this.lazyConnection.Start();
            }
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SettingsAppearValid")
            {
                this.NotifyOfPropertyChange(() => this.SettingsAppearValid);
            }

            if (e.PropertyName == "TivoIPAddress" ||
                e.PropertyName == "MediaAccessKey" ||
                e.PropertyName == "Username" ||
                e.PropertyName == "Password")
            {
                ResetConnection();
            }

            if (e.PropertyName == "IsTestInProgress")
            {
                this.IsConnectionEnabled = !this.settings.IsTestInProgress;
            }
        }

        public bool SettingsAppearValid
        {
            get
            {
                return this.settings.SettingsAppearValid;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public bool IsAwayModeEnabled
        {
            get
            {
                return this.isAwayModeEnabled;
            }

            set
            {
                if (this.isAwayModeEnabled == value)
                {
                    return;
                }

                this.isAwayModeEnabled = value;

                ResetConnection();

                this.NotifyOfPropertyChange(() => this.IsAwayModeEnabled);
            }
        }

        public string Error
        {
            get
            {
                return this.error;
            }

            set
            {
                this.error = value;
                this.NotifyOfPropertyChange(() => this.Error);
            }
        }

        public Task<TivoConnection> GetConnectionAsync()
        {
            if (this.lazyConnection == null)
            {
                return TaskEx.FromResult<TivoConnection>(null);
            }

            return this.lazyConnection.Value;
        }

        private async Task<TivoConnection> ConnectAsync()
        {
            if (!this.SettingsAppearValid ||
                !this.IsConnectionEnabled)
            {
                return null;
            }

            this.eventAggregator.Publish(new TivoOperationStarted());

            var localConnection = new TivoConnection(this.sterlingInstance.Database);
            try
            {
                if (this.IsAwayModeEnabled)
                {
                    await localConnection.ConnectAway(this.settings.Username, this.settings.Password);
                }
                else
                {
                    await localConnection.Connect(this.settings.ParsedIPAddress, this.settings.MediaAccessKey);
                }

                this.isConnected = true;
                NotifyOfPropertyChange(() => IsConnected);

                return localConnection;
            }
            catch (Exception ex)
            {
                localConnection.Dispose();

                this.isConnected = false;
                this.Error = ex.Message;

                NotifyOfPropertyChange(() => IsConnected);

                return null;
            }
            finally
            {
                this.eventAggregator.Publish(new TivoOperationFinished());
            }
        }
    }
}
