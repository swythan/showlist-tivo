//-----------------------------------------------------------------------
// <copyright file="TivoConnectionService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Nito.AsyncEx;
using Tivo.Connect;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Settings;
using TivoAhoy.Common.Utils;

namespace TivoAhoy.Common.Services
{
    public class TivoConnectionService : PropertyChangedBase, ITivoConnectionService, IHandle<ConnectionSettingsChanged>
    {
        private readonly IAnalyticsService analyticsService;
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;

        private bool isConnectionEnabled = false;

        private AsyncLazy<TivoConnection> lazyConnection;

        private bool isConnected = false;
        private bool isAwayMode = false;

        private string error;

        public TivoConnectionService(
            IAnalyticsService analyticsService,
            IEventAggregator eventAggregator,
            IProgressService progressService)
        {
            this.analyticsService = analyticsService;
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

        public async Task<TivoConnection> GetConnectionAsync()
        {
            if (this.lazyConnection == null)
            {
                return null;
            }

            return await this.lazyConnection;
        }


        private static Tuple<string, Stream> LoadCertificateAndPassword(bool isVirginMedia)
        {
            // Load the cert
            if (isVirginMedia)
            {
                var stream = typeof(TivoConnectionService).Assembly.GetManifestResourceStream("TivoAhoy.Common.tivo_vm.p12");
                return Tuple.Create("R2N48DSKr2Cm", stream);
            }
            else
            {
                var stream = typeof(TivoConnectionService).Assembly.GetManifestResourceStream("TivoAhoy.Common.tivo_us.p12");
                return Tuple.Create("mpE7Qy8cSqdf", stream);
            }
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

                // TODO: detect this based on the Tivo mDNS data
                var certs = LoadCertificateAndPassword(false);
                var middleMind = false ? @"secure-tivo-api.virginmedia.com" : "middlemind.tivo.com";

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
                            await localConnection.Connect(lanSettings.LastIpAddress.ToString(), lanSettings.MediaAccessKey, certs.Item2, certs.Item1);

                            this.isConnected = true;
                            this.isAwayMode = false;

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

                            this.analyticsService.ConnectedHomeMode();

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
                        await localConnection.ConnectAway(ConnectionSettings.AwayModeUsername, 
                                                          ConnectionSettings.AwayModePassword,
                                                          middleMind, 
                                                          false,
                                                          certs.Item2, 
                                                          certs.Item1);

                        this.isConnected = true;
                        this.isAwayMode = true;

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

                        this.analyticsService.ConnectedAwayMode();
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
