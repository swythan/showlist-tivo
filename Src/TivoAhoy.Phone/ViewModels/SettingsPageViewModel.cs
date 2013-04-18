using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Phone.Net.NetworkInformation;
using Tivo.Connect;
using TivoAhoy.Phone.Discovery;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class SettingsPageViewModel : Screen
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        private readonly IEventAggregator eventAggregator;
        private readonly ITivoConnectionService connectionService;

        private KnownTivoConnection lanSettings;

        private string username;
        private string password;

        private bool isTestInProgress;

        private ObservableCollection<DiscoveredTivo> discoveredTivos = new ObservableCollection<DiscoveredTivo>();
        private DiscoveredTivo currentDiscoveredTivo;

        public SettingsPageViewModel(IEventAggregator eventAggregator, ITivoConnectionService connectionService)
        {
            this.eventAggregator = eventAggregator;
            this.connectionService = connectionService;

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

        private void OnOperationStarted()
        {
            this.isTestInProgress = true;
            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanSearchLAN);
            NotifyOfPropertyChange(() => this.CanTestLANConnection);
            NotifyOfPropertyChange(() => this.CanTestAwayConnection);
        }

        private void OnOperationFinished()
        {
            this.isTestInProgress = false;
            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanSearchLAN);
            NotifyOfPropertyChange(() => this.CanTestLANConnection);
            NotifyOfPropertyChange(() => this.CanTestAwayConnection);
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

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.Username, this.Password);

                ConnectionSettings.AwayModeUsername = this.Username;
                ConnectionSettings.AwayModePassword = this.Password;
                this.eventAggregator.Publish(new ConnectionSettingsChanged());

                MessageBox.Show("Connection Succeeded!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                connection.Dispose();
                OnOperationFinished();
            }
        }

        public async void TestLANConnection()
        {
            var connection = new TivoConnection();

            OnOperationStarted();

            try
            {
                await connection.Connect(this.LanSettings.LastIpAddress, this.LanSettings.MediaAccessKey);

                if (!this.LanSettings.TSN.Equals(connection.ConnectedTsn, StringComparison.Ordinal))
                {
                    this.LanSettings.TSN = connection.ConnectedTsn;
                }

                var knownTivosByTsn = ConnectionSettings.KnownTivos.ToDictionary(x => x.TSN);

                knownTivosByTsn[connection.ConnectedTsn] = this.LanSettings;

                ConnectionSettings.KnownTivos = knownTivosByTsn.Values.ToArray();
                ConnectionSettings.SelectedTivoTsn = connection.ConnectedTsn;

                this.eventAggregator.Publish(new ConnectionSettingsChanged());

                MessageBox.Show("Connection Succeeded!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Connection Failed :\n{0}", ex.Message));
            }
            finally
            {
                connection.Dispose();
                OnOperationFinished();
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

            OnOperationStarted();

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

                var selectedTivo = this.discoveredTivos
                    .FirstOrDefault(x => x.TSN.Equals(ConnectionSettings.SelectedTivoTsn, StringComparison.Ordinal));

                if (selectedTivo == null)
                {
                    selectedTivo = this.discoveredTivos.FirstOrDefault();
                }

                this.CurrentDiscoveredTivo = selectedTivo;
            }

            OnOperationFinished();
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
    }
}
