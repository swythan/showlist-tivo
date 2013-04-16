using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect;
using TivoAhoy.Phone.Events;
using TivoAhoy.Phone.ViewModels;

namespace TivoAhoy.Phone
{
    public class TivoConnectionService : PropertyChangedBase, ITivoConnectionService, IHandle<ConnectionSettingsChanged>
    {
        private readonly IEventAggregator eventAggregator;

        private bool isAwayModeEnabled = false;
        private bool isConnectionEnabled = false;

        private AsyncLazy<TivoConnection> lazyConnection;
        private bool isConnected = false;
        private string error;

        public TivoConnectionService(
            IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
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
                this.lazyConnection = new AsyncLazy<TivoConnection>(async() => 
                    {
                        var conn = await this.ConnectAsync();
                        if (conn == null &&
                            !this.IsAwayModeEnabled)
                        {
                            this.IsAwayModeEnabled = true;
                            conn = await this.ConnectAsync();
                        }

                        return conn;
                    });

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

            var localConnection = new TivoConnection();
            try
            {
                if (this.IsAwayModeEnabled)
                {
                    await localConnection.ConnectAway(ConnectionSettings.AwayModeUsername, ConnectionSettings.AwayModePassword);
                }
                else
                {
                    var lanSettings = ConnectionSettings.KnownTivos
                        .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.Ordinal));

                    if (lanSettings != null)
                    {
                        await localConnection.Connect(lanSettings.LastIpAddress, lanSettings.MediaAccessKey);
                    }
                    else
                    {
                        return null;
                    }
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
