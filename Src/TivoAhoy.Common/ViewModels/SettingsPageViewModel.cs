//-----------------------------------------------------------------------
// <copyright file="SettingsPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Tivo.Connect;
using TivoAhoy.Common.Discovery;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Settings;

namespace TivoAhoy.Common.ViewModels
{
    public class SettingsPageViewModel : Screen
    {
        private readonly IDiscoveryService discoveryService;
        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private KnownTivoConnection lanSettings;

        private string username;
        private string password;

        private bool isTestInProgress;

        private readonly ObservableCollection<DiscoveredTivo> discoveredTivos = new ObservableCollection<DiscoveredTivo>();
        private DiscoveredTivo currentDiscoveredTivo;
        private TivoServiceProvider awayServiceProvider;

        public SettingsPageViewModel(
            IEventAggregator eventAggregator, 
            IProgressService progressService, 
            ITivoConnectionService connectionService,
            IDiscoveryService discoveryService)
        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;
            this.discoveryService = discoveryService;
            this.progressService = progressService;

            this.connectionService.PropertyChanged += OnConnectionServicePropertyChanged;

            // default awayServiceProvider to defaults based on language
            var culture = CultureInfo.CurrentUICulture;
            if(culture.Name.StartsWith("en-US"))
                awayServiceProvider = TivoServiceProvider.TivoUSA;
            else
                awayServiceProvider = TivoServiceProvider.VirginMediaUK; 
        }

        private void OnConnectionServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ConnectedNetworkName")
            {
                NotifyOfPropertyChange(() => this.CanSearchLAN);
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            Observable.Timer(TimeSpan.FromSeconds(0.5))
                .ObserveOnDispatcher()
                .Subscribe(_ =>
                    {
                        if (this.CanSearchLAN)
                        {
                            this.SearchLAN();
                        }
                    });

            this.Username = ConnectionSettings.AwayModeUsername;
            this.Password = ConnectionSettings.AwayModePassword;
            this.AwayServiceProvider = ConnectionSettings.AwayModeServiceProvider;
        }

        protected override void OnDeactivate(bool close)
        {
            if (!close)
            {
                //ConnectionSettings.TivoIPAddress = this.TivoIPAddress;
                //ConnectionSettings.MediaAccessKey = this.MediaAccessKey;
                //ConnectionSettings.AwayModeUsername = this.Username;
                //ConnectionSettings.AwayModePassword = this.Password;
                this.eventAggregator.Publish(new ConnectionSettingsChanged());
            }

            base.OnDeactivate(close);
        }

        public ObservableCollection<DiscoveredTivo> DiscoveredTivos
        {
            get { return this.discoveredTivos; }
        }

        public DiscoveredTivo CurrentDiscoveredTivo
        {
            get { return this.currentDiscoveredTivo; }
            set
            {
                if (this.currentDiscoveredTivo == value)
                    return;

                this.currentDiscoveredTivo = value;

                if (this.currentDiscoveredTivo != null)
                {
                    var settings = ConnectionSettings.KnownTivos
                        .FirstOrDefault(x => x.TSN.Equals(this.currentDiscoveredTivo.TSN, StringComparison.Ordinal));

                    if (settings == null)
                    {
                        settings = new KnownTivoConnection()
                        {
                            Name = this.currentDiscoveredTivo.Name,
                            TSN = this.currentDiscoveredTivo.TSN,
                        };
                    }

                    settings.IsVirginMedia = this.currentDiscoveredTivo.IsVirginMedia;
                    settings.LastIpAddress = this.currentDiscoveredTivo.IpAddress;
                    settings.NetworkName = this.connectionService.ConnectedNetworkName;

                    this.LanSettings = settings;
                }
                else
                {
                    this.LanSettings = ConnectionSettings.KnownTivos
                        .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.Ordinal));
                }

            }
        }

        public KnownTivoConnection LanSettings
        {
            get { return this.lanSettings; }
            set
            {
                if (this.lanSettings == value)
                    return;

                this.lanSettings = value;
                this.NotifyOfPropertyChange(() => this.LanSettings);
                this.NotifyOfPropertyChange(() => this.LanName);
                this.NotifyOfPropertyChange(() => this.LanIpAddress);
                this.NotifyOfPropertyChange(() => this.LanMediaAccessKey);
                this.NotifyOfPropertyChange(() => this.CanTestLANConnection);
                this.NotifyOfPropertyChange(() => this.LanSettingsAppearValid);
            }
        }

        public string LanName
        {
            get
            {
                if (this.LanSettings == null)
                {
                    return null;
                }

                return this.lanSettings.Name;
            }
        }

        public string LanIpAddress
        {
            get
            {
                if (this.LanSettings == null)
                {
                    return null;
                }

                return this.lanSettings.LastIpAddress.ToString();
            }
        }

        public string LanMediaAccessKey
        {
            get
            {
                if (this.LanSettings == null)
                {
                    return null;
                }

                return this.lanSettings.MediaAccessKey;
            }

            set
            {
                if (this.LanSettings == null)
                {
                    return;
                }

                if (this.LanSettings.MediaAccessKey == value)
                {
                    return;
                }

                this.LanSettings.MediaAccessKey = value;
                this.NotifyOfPropertyChange(() => this.LanMediaAccessKey);
                this.NotifyOfPropertyChange(() => this.CanTestLANConnection);
                this.NotifyOfPropertyChange(() => this.LanSettingsAppearValid);
            }
        }

        public string Username
        {
            get { return this.username; }
            set
            {
                if (this.username == value)
                    return;

                this.username = value;
                NotifyOfPropertyChange(() => this.Username);
                NotifyOfPropertyChange(() => this.CanTestAwayConnection);
                NotifyOfPropertyChange(() => this.AwaySettingsAppearValid);
            }
        }

        public string Password
        {
            get { return this.password; }
            set
            {
                if (this.password == value)
                    return;

                this.password = value;
                NotifyOfPropertyChange(() => this.Password);
                NotifyOfPropertyChange(() => this.CanTestAwayConnection);
                NotifyOfPropertyChange(() => this.AwaySettingsAppearValid);
            }
        }

        public TivoServiceProvider AwayServiceProvider
        {
            get { return this.awayServiceProvider; }
            set
            {
                if (this.awayServiceProvider == value)
                    return;

                this.awayServiceProvider = value;
                NotifyOfPropertyChange(() => this.AwayServiceProvider);
                NotifyOfPropertyChange(() => this.CanTestAwayConnection);
                NotifyOfPropertyChange(() => this.AwaySettingsAppearValid);
            }
        }

        public bool IsTestInProgress
        {
            get { return this.isTestInProgress; }
        }

        private void SetIsTestInProgress(bool value)
        {
            if (this.isTestInProgress == value)
            {
                return;
            }

            this.isTestInProgress = value;

            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanSearchLAN);
            NotifyOfPropertyChange(() => this.CanTestLANConnection);
            NotifyOfPropertyChange(() => this.CanTestAwayConnection);
        }

        private IDisposable ShowProgress()
        {
            this.SetIsTestInProgress(true);

            return new CompositeDisposable(
                this.progressService.Show(),
                Disposable.Create(() => this.SetIsTestInProgress(false)));
        }

        public bool CanTestAwayConnection
        {
            get
            {
                return this.AwaySettingsAppearValid && !this.IsTestInProgress;
            }
        }

        public bool CanTestLANConnection
        {
            get
            {
                return this.LanSettingsAppearValid && !this.IsTestInProgress;
            }
        }

        public bool LanSettingsAppearValid
        {
            get
            {
                if (this.LanSettings == null)
                {
                    return false;
                }

                return ConnectionSettings.LanSettingsAppearValid(this.LanSettings.LastIpAddress, this.LanSettings.MediaAccessKey);
            }
        }

        public bool AwaySettingsAppearValid
        {
            get
            {
                return ConnectionSettings.AwaySettingsAppearValid(this.Username, this.Password);
            }
        }

        public async void TestAwayConnection()
        {
            var connection = new TivoConnection();

            try
            {
                using (ShowProgress())
                {
                    await connection.ConnectAway(this.Username, this.Password, this.AwayServiceProvider, TivoCertificateStore.Instance);

                    ConnectionSettings.AwayModeUsername = this.Username;
                    ConnectionSettings.AwayModePassword = this.Password;
                    ConnectionSettings.AwayModeServiceProvider = this.AwayServiceProvider;
                    this.eventAggregator.Publish(new ConnectionSettingsChanged());
                }

                var toast = new ToastPrompt()
                {
                    Title = "Connection succeeded",
                    Message = "Away Mode",
                    TextOrientation = Orientation.Vertical,
                };

                toast.Show();
            }
            catch (UnauthorizedAccessException)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = "Invalid username or password",
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (TivoException ex)
            {
                string message = ex.Message;

                if (ex.Code == "authenticationFailed")
                {
                    message = "Invalid username or password";
                }

                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (Exception ex)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = ex.Message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            finally
            {
                connection.Dispose();
            }
        }

        public async void TestLANConnection()
        {
            var connection = new TivoConnection();

            try
            {
                using (ShowProgress())
                {
                    var service = lanSettings.IsVirginMedia == false ? TivoServiceProvider.TivoUSA : TivoServiceProvider.VirginMediaUK;
                    await connection.Connect(this.LanSettings.LastIpAddress.ToString(), this.LanSettings.MediaAccessKey, service, TivoCertificateStore.Instance);

                    if (!this.LanSettings.TSN.Equals(connection.ConnectedTsn, StringComparison.Ordinal))
                    {
                        this.LanSettings.TSN = connection.ConnectedTsn;
                    }

                    var knownTivosByTsn = ConnectionSettings.KnownTivos.ToDictionary(x => x.TSN);

                    knownTivosByTsn[connection.ConnectedTsn] = this.LanSettings;

                    ConnectionSettings.KnownTivos = knownTivosByTsn.Values.ToArray();
                    ConnectionSettings.SelectedTivoTsn = connection.ConnectedTsn;

                    this.eventAggregator.Publish(new ConnectionSettingsChanged());
                }

                var toast = new ToastPrompt()
                {
                    Title = "Connection succeeded",
                    Message = "Home Mode",
                    TextOrientation = Orientation.Vertical,
                };

                toast.Show();
            }
            catch (ActionNotSupportedException)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = "Network remote control not enabled on TiVo.",
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (UnauthorizedAccessException)
            {
                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = "Incorrect Media Access Key",
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (TivoException ex)
            {
                string message = ex.Message;

                if (ex.Code == "authenticationFailed")
                {
                    message = "Incorrect Media Access Key";
                }

                var toast = new ToastPrompt()
                {
                    Title = "Connection Failed",
                    Message = message,
                    TextOrientation = Orientation.Vertical,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Colors.Red),
                };

                toast.Show();
            }
            catch (Exception)
            {
            }
            finally
            {
                connection.Dispose();
            }
        }

        public bool CanSearchLAN
        {
            get
            {
                if (this.IsTestInProgress)
                    return false;

                //return true; // use this for testing in emulator
               return this.connectionService.ConnectedNetworkName != null;
            }
        }

        public async void SearchLAN()
        {
            if (!this.CanSearchLAN)
                return;

            this.discoveredTivos.Clear();

            using (ShowProgress())
            {
                var progress = new Microsoft.Progress<DiscoveredTivo>(x => this.discoveredTivos.Add(x));
                
                await discoveryService.DiscoverTivosAsync(progress, CancellationToken.None);

                if (this.discoveredTivos.Any())
                {
                    var selectedTivo = this.discoveredTivos
                        .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.OrdinalIgnoreCase));

                    if (selectedTivo == null)
                    {
                        selectedTivo = this.discoveredTivos.FirstOrDefault();
                    }

                    this.CurrentDiscoveredTivo = selectedTivo;
                }
            }
        }
    }
}
