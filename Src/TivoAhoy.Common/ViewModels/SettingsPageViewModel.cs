//-----------------------------------------------------------------------
// <copyright file="SettingsPageViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Tivo.Connect;
using TivoAhoy.Common.Discovery;
using TivoAhoy.Common.Events;
using TivoAhoy.Common.Services;
using TivoAhoy.Common.Settings;


#if !WP7
using Zeroconf;
#endif

namespace TivoAhoy.Common.ViewModels
{
    public class SettingsPageViewModel : Screen
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        private readonly IEventAggregator eventAggregator;
        private readonly IProgressService progressService;
        private readonly ITivoConnectionService connectionService;

        private KnownTivoConnection lanSettings;

        private string username;
        private string password;

        private bool isTestInProgress;

        private ObservableCollection<DiscoveredTivo> discoveredTivos = new ObservableCollection<DiscoveredTivo>();
        private DiscoveredTivo currentDiscoveredTivo;

        public SettingsPageViewModel(
            IEventAggregator eventAggregator, 
            IProgressService progressService, 
            ITivoConnectionService connectionService)
        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;
            this.progressService = progressService;

            this.connectionService.PropertyChanged += OnConnectionServicePropertyChanged;
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

        public async void TestAwayConnection()
        {
            var connection = new TivoConnection();

            // TODO: Detect this
            var cert = LoadCertificateAndPassword(false);
            var middleMind = false ? @"secure-tivo-api.virginmedia.com" : "middlemind.tivo.com";
            try
            {
                using (ShowProgress())
                {
                    await connection.ConnectAway(this.Username, this.Password, middleMind, cert.Item2, cert.Item1);

                    ConnectionSettings.AwayModeUsername = this.Username;
                    ConnectionSettings.AwayModePassword = this.Password;
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
            catch (UnauthorizedAccessException ex)
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
            // TODO: Detect this
            var cert = LoadCertificateAndPassword(false);

            try
            {
                using (ShowProgress())
                {
                    await connection.Connect(this.LanSettings.LastIpAddress.ToString(), this.LanSettings.MediaAccessKey, cert.Item2, cert.Item1);

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
            catch (Exception ex)
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

                var discovered = await DiscoverTivos();

                if (discovered)
                {
                    var selectedTivo = this.discoveredTivos
                                            .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, 
                                                StringComparison.Ordinal));

                    if (selectedTivo == null)
                    {
                        selectedTivo = this.discoveredTivos.FirstOrDefault();
                    }

                    this.CurrentDiscoveredTivo = selectedTivo;
                }
            }
        }

#if WP7

        private async Task<bool> DiscoverTivos()
        {
            var dnsAnswers = await MDnsClient.CreateAndResolveAsync(dnsProtocol);

            if (dnsAnswers != null)
            {
                var answersSubscription = dnsAnswers
                    .Select(HandleDnsAnswer)
                    .Where(x => x != null)
                    .ObserveOnDispatcher()
                    .Subscribe(x => this.discoveredTivos.Add(x));

                await TaskEx.Delay(TimeSpan.FromSeconds(3));

                answersSubscription.Dispose();

                return true;
            }
            return false;
        }

        private DiscoveredTivo HandleDnsAnswer(Discovery.Message msg)
        {
            bool isTivoResponse = false;

            foreach (var answer in msg.Answers)
            {
                if (answer.Type == Discovery.Type.PTR)
                {
                    var ptrData = (Discovery.Ptr)answer.ResponseData;
                    var name = ptrData.DomainName.ToString();

                    if (name.Contains(dnsProtocol))
                    {
                        isTivoResponse = true;
                    }
                }
            }

            if (!isTivoResponse)
            {
                return null;
            }

            int? port = null;
            IPAddress tivoAddress = null;
            string tivoName = null;
            string tivoTsn = null;

            foreach (var additional in msg.Additionals)
            {
                if (additional.Type == Discovery.Type.TXT)
                {
                    var txtData = (Discovery.Txt)additional.ResponseData;

                    foreach (var property in txtData.Properties)
                    {
                        Debug.WriteLine("TXT entry: {0} = {1}", property.Key, property.Value);
                        if (property.Key == "TSN")
                        {
                            tivoTsn = property.Value;
                        }
                    }
                }

                if (additional.Type == Discovery.Type.A)
                {
                    var hostAddress = (Discovery.HostAddress)additional.ResponseData;

                    tivoAddress = hostAddress.Address;
                    tivoName = additional.DomainName[0];
                }

                if (additional.Type == Discovery.Type.SRV)
                {
                    var srvData = (Discovery.Srv)additional.ResponseData;

                    port = srvData.Port;
                }
            }

            Debug.WriteLine("TiVo found at {0}:{1}", msg.From.Address, port);

            return new DiscoveredTivo(tivoName, tivoAddress, tivoTsn);
        }
#else
        private async Task<bool> DiscoverTivos()
        {
            const string tivoService = "_tivo-mindrpc._tcp.local.";
            var results = await ZeroconfResolver.ResolveAsync(tivoService, TimeSpan.FromSeconds(.5));

            if (results.Count > 0)
            {
                foreach (var host in results)
                {
                    var device = new DiscoveredTivo(host.DisplayName, 
                        IPAddress.Parse(host.IPAddress), 
                        host.Services.First().Value.Properties[0]["TSN"]);

                    this.discoveredTivos.Add(device);
                }
                return true;
            }
            return false;
        }
#endif
    }
}
