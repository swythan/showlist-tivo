//-----------------------------------------------------------------------
// <copyright file="TivoConnectionService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Tivo.Connect;
using TivoAhoy.Common.Discovery;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Settings;

namespace TivoAhoy.Common.Services
{
    public class TivoConnectionService : PropertyChangedBase, ITivoConnectionService, IHandle<ConnectionSettingsChanged>
    {
        private readonly IAnalyticsService analyticsService;
        private readonly IDiscoveryService discoveryService;
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;

        private bool isConnectionEnabled = false;

        private TivoConnection awayConnection;
        private TivoConnection homeConnection;

        private string error;
        private Task connectionTask;

        public TivoConnectionService(
            IAnalyticsService analyticsService,
            IDiscoveryService discoveryService,
            IEventAggregator eventAggregator,
            IProgressService progressService)
        {
            this.analyticsService = analyticsService;
            this.discoveryService = discoveryService;
            this.eventAggregator = eventAggregator;
            this.progressService = progressService;

            this.eventAggregator.Subscribe(this);

            DeviceNetworkInformation.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        public async Task<bool> EnsureConnectedAsync()
        {
            if (!IsConnectionEnabled)
            {
                return false;
            }

            if (connectionTask == null)
            {
                connectionTask = AutoConnect();
            }

            await connectionTask.ConfigureAwait(true);

            return this.IsConnected;
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
            if (e.NotificationType == NetworkNotificationType.InterfaceConnected ||
                e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
            {
                if (e.NetworkInterface.InterfaceSubtype == NetworkInterfaceSubType.WiFi)
                {
                    NotifyOfPropertyChange(() => this.ConnectedNetworkName);
                }

                if (e.NotificationType == NetworkNotificationType.InterfaceConnected)
                {
                    this.connectionTask = AutoConnect();
                }

                if (e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
                {
                    ResetConnection();
                }
            }
        }

        private void ResetConnection()
        {
            this.awayConnection = null;
            this.homeConnection = null;

            this.NotifyOfPropertyChange(() => this.IsConnected);

            this.connectionTask = AutoConnect();
        }

        private async Task AutoConnect()
        {
            bool wasAwayConnectedAlready = this.awayConnection != null;
            bool wasHomeConnectedAlready = this.homeConnection != null;
 
            if (this.SettingsAppearValid && this.IsConnectionEnabled)
            {
                Task<IEnumerable<DiscoveredTivo>> discoveryTask;
                if (this.ConnectedNetworkName != null)
                {
                    discoveryTask = this.discoveryService.DiscoverTivosAsync(null, CancellationToken.None);
                }
                else
                {
                    discoveryTask = TaskEx.FromResult(Enumerable.Empty<DiscoveredTivo>());
                }

                // TODO: detect this based on the Tivo mDNS data
                var service = TivoServiceProvider.VirginMediaUK;

                string username = ConnectionSettings.AwayModeUsername;
                string password = ConnectionSettings.AwayModePassword;

                if (this.awayConnection == null)
                {
                    this.awayConnection = await this.ConnectAwayModeAsync(username, password, service);
                }

                var found = await discoveryTask;
                if (this.awayConnection != null)
                {
                    var accountTivos = this.awayConnection.AssociatedTivos;

                    var availableTivos = accountTivos.Join(
                        found, 
                        b => b.Id.Substring(4), 
                        t => t.TSN, 
                        (b, t) => new { t.Name, t.TSN, t.IpAddress, b.FriendlyName },
                        StringComparer.OrdinalIgnoreCase);

                    var firstAvailable = availableTivos.FirstOrDefault();

                    if (firstAvailable != null)
                    {
                        this.homeConnection = await this.ConnectHomeModeAsync(firstAvailable.IpAddress, this.awayConnection.MediaAccessKey, service);
                    }
                }
            }

            this.NotifyOfPropertyChange(() => this.IsConnected);
            this.NotifyOfPropertyChange(() => this.IsHomeMode);


            if (this.homeConnection != null && !wasHomeConnectedAlready)
            {
                Execute.BeginOnUIThread(() =>
                {
                    var toast = new ToastPrompt()
                    {
                        Title = "Connected",
                        Message = "Home Mode",
                        MillisecondsUntilHidden = 600,
                    };

                    toast.Show();
                });
            }
            else
            {
                if (this.awayConnection != null && !wasAwayConnectedAlready)
                {
                    Execute.BeginOnUIThread(() =>
                    {
                        var toast = new ToastPrompt()
                        {
                            Title = "Connected",
                            Message = "Away Mode",
                            MillisecondsUntilHidden = 600,
                        };

                        toast.Show();
                    });
                }
            }
        }

        async void IHandle<ConnectionSettingsChanged>.Handle(ConnectionSettingsChanged message)
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

                return false;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.awayConnection != null || this.homeConnection != null;
            }
        }

        public bool IsHomeMode
        {
            get
            {
                return this.homeConnection != null;
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

        public TivoConnection Connection
        {
            get
            {
                if (this.homeConnection != null)
                {
                    return homeConnection;
                }

                return this.awayConnection;
            }
        }

        private async Task<TivoConnection> ConnectAwayModeAsync(
            string username,
            string password,
            TivoServiceProvider serviceProvider)
        {
            if (!this.IsConnectionEnabled)
            {
                return null;
            }

            if (!ConnectionSettings.AwaySettingsAppearValid(username, password))
            {
                return null;
            }

            using (this.progressService.Show())
            {
                var localConnection = new TivoConnection();

                try
                {
                    await localConnection.ConnectAway(username, password, serviceProvider, TivoCertificateStore.Instance);

                    this.analyticsService.ConnectedAwayMode();
                }
                catch (Exception ex)
                {
                    this.Error = ex.Message;

                    localConnection.Dispose();
                    localConnection = null;
                }
                
                return localConnection;
            }
        }

        private async Task<TivoConnection> ConnectHomeModeAsync(
            IPAddress serverAddress,
            string mediaAccessKey,
            TivoServiceProvider serviceProvider)
        {
            if (!this.IsConnectionEnabled)
            {
                return null;
            }

            if (!ConnectionSettings.LanSettingsAppearValid(serverAddress, mediaAccessKey))
            {
                return null;
            }

            using (this.progressService.Show())
            {
                var localConnection = new TivoConnection();
                try
                {
                    await localConnection.Connect(serverAddress.ToString(), mediaAccessKey, serviceProvider, TivoCertificateStore.Instance);

                    this.analyticsService.ConnectedHomeMode();
                }
                catch (Exception ex)
                {
                    this.Error = ex.Message;

                    localConnection.Dispose();
                    localConnection = null;
                }

                return localConnection;
            }
        }
    }
}
