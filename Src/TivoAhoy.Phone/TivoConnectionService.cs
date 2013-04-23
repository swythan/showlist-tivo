using System;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Microsoft.Phone.Net.NetworkInformation;
using Tivo.Connect;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone
{
    public class TivoConnectionService : PropertyChangedBase, ITivoConnectionService, IHandle<ConnectionSettingsChanged>
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;

        private bool isConnectionEnabled = false;

        private AsyncLazy<TivoConnection> lazyConnection;

        private bool isConnected = false;
        private bool isAwayMode = false;

        private string error;

        public TivoConnectionService(
            IEventAggregator eventAggregator,
            IProgressService progressService)
        {
            this.eventAggregator = eventAggregator;
            this.progressService = progressService;

            this.eventAggregator.Subscribe(this);

            DeviceNetworkInformation.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
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

        public string ConnectedNetworkName
        {
            get
            {
                using (var networks = new NetworkInterfaceList())
                {
                    foreach (var network in networks)
                    {
                        if (network.InterfaceState == ConnectState.Connected &&
                            network.InterfaceSubtype == NetworkInterfaceSubType.WiFi)
                        {
                            return network.InterfaceName;
                        }
                    }
                }

                return null;
            }
        }

        private void OnNetworkAvailabilityChanged(object sender, NetworkNotificationEventArgs e)
        {
            if (e.NetworkInterface.InterfaceSubtype == NetworkInterfaceSubType.WiFi)
            {
                if (e.NotificationType == NetworkNotificationType.InterfaceConnected ||
                    e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
                {
                    NotifyOfPropertyChange(() => this.ConnectedNetworkName);
                }
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
                this.lazyConnection = new AsyncLazy<TivoConnection>(async () => await this.ConnectAsync(false));

                await TaskEx.Delay(TimeSpan.FromSeconds(2));

                this.lazyConnection.Start();
            }
        }


        void IHandle<ConnectionSettingsChanged>.Handle(ConnectionSettingsChanged message)
        {
            this.NotifyOfPropertyChange(() => this.SettingsAppearValid);
            ResetConnection();
        }

        public bool SettingsAppearValid
        {
            get
            {
                if (ConnectionSettings.AwaySettingsAppearValid(ConnectionSettings.AwayModeUsername, ConnectionSettings.AwayModePassword))
                {
                    return true;
                }

                var lanSettings = ConnectionSettings.KnownTivos
                    .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.Ordinal));

                if (lanSettings == null)
                {
                    return false;
                }

                if (lanSettings.NetworkName != this.ConnectedNetworkName)
                {
                    return false;
                }

                return ConnectionSettings.LanSettingsAppearValid(lanSettings.LastIpAddress, lanSettings.MediaAccessKey);
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        public bool IsAwayMode
        {
            get
            {
                return this.isAwayMode;
            }

            set
            {
                if (this.isAwayMode == value)
                {
                    return;
                }

                this.isAwayMode = value;

                ResetConnection();

                this.NotifyOfPropertyChange(() => this.IsAwayMode);
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

        private async Task<TivoConnection> ConnectAsync(bool forceAwayMode)
        {
            if (!this.SettingsAppearValid ||
                !this.IsConnectionEnabled)
            {
                return null;
            }

            using (this.progressService.Show())
            {
                this.isConnected = false;
                this.isAwayMode = false;

                var localConnection = new TivoConnection();

                if (!forceAwayMode)
                {
                    var lanSettings = ConnectionSettings.KnownTivos
                        .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.Ordinal));

                    if (lanSettings != null &&
                        ConnectionSettings.LanSettingsAppearValid(lanSettings.LastIpAddress, lanSettings.MediaAccessKey) &&
                        lanSettings.NetworkName == this.ConnectedNetworkName)
                    {
                        try
                        {
                            await localConnection.Connect(lanSettings.LastIpAddress, lanSettings.MediaAccessKey);

                            this.isConnected = true;
                            this.isAwayMode = false;
                        }
                        catch (Exception ex)
                        {
                            this.Error = ex.Message;
                        }
                    }
                }

                if (!this.isConnected)
                {
                    try
                    {
                        await localConnection.ConnectAway(ConnectionSettings.AwayModeUsername, ConnectionSettings.AwayModePassword);

                        this.isConnected = true;
                        this.isAwayMode = true;
                    }
                    catch (Exception ex)
                    {
                        this.Error = ex.Message;
                    }
                }

                NotifyOfPropertyChange(() => IsConnected);
                NotifyOfPropertyChange(() => IsAwayMode);

                if (!this.isConnected)
                {
                    localConnection.Dispose();
                    localConnection = null;
                }

                return localConnection;
            }
        }
    }
}
