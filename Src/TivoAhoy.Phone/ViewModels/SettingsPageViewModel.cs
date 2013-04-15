using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using TivoAhoy.Phone.Discovery;
using TivoAhoy.Phone.Events;

namespace TivoAhoy.Phone.ViewModels
{
    public class SettingsPageModelStorage : StorageHandler<SettingsPageViewModel>
    {
        public override void Configure()
        {
            this.Property(x => x.TivoIPAddress)
                .InPhoneState();

            this.Property(x => x.MediaAccessKey)
                .InPhoneState();

            this.Property(x => x.Username)
                .InPhoneState();

            this.Property(x => x.Password)
                .InPhoneState();
        }
    }

    public class SettingsPageViewModel : Screen
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        private readonly IEventAggregator eventAggregator;

        private string tivoIPAddress;
        private string mediaAccessKey;
        private string username;
        private string password;
        private bool isTestInProgress;

        private Dictionary<string, IPAddress> discoveredTivos = new Dictionary<string, IPAddress>();
        private object syncLock = new object();

        public SettingsPageViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        protected override void OnInitialize()
        {
            this.TivoIPAddress = ConnectionSettings.TivoIPAddress;
            this.MediaAccessKey = ConnectionSettings.MediaAccessKey;
            this.Username = ConnectionSettings.Username;
            this.Password = ConnectionSettings.Password;

            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            if (!close)
            {
                ConnectionSettings.TivoIPAddress = this.TivoIPAddress;
                ConnectionSettings.MediaAccessKey = this.MediaAccessKey;
                ConnectionSettings.Username = this.Username;
                ConnectionSettings.Password = this.Password;
                this.eventAggregator.Publish(new ConnectionSettingsChanged());
            }

            base.OnDeactivate(close);
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
                NotifyOfPropertyChange(() => this.ParsedIPAddress);
                NotifyOfPropertyChange(() => this.CanTestLANConnection);
                NotifyOfPropertyChange(() => this.LanSettingsAppearValid);
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
                NotifyOfPropertyChange(() => this.CanTestLANConnection);
                NotifyOfPropertyChange(() => this.LanSettingsAppearValid);
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
            NotifyOfPropertyChange(() => this.CanTestLANConnection);
            NotifyOfPropertyChange(() => this.CanTestAwayConnection);
        }

        private void OnOperationFinished()
        {
            this.isTestInProgress = false;
            NotifyOfPropertyChange(() => this.IsTestInProgress);
            NotifyOfPropertyChange(() => this.CanTestLANConnection);
            NotifyOfPropertyChange(() => this.CanTestAwayConnection);
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
                return ConnectionSettings.LanSettingsAppearValid(this.TivoIPAddress, this.MediaAccessKey);
            }
        }

        public bool AwaySettingsAppearValid
        {
            get
            {
                return ConnectionSettings.LanSettingsAppearValid(this.TivoIPAddress, this.MediaAccessKey);
            }
        }

        public async void TestAwayConnection()
        {
            var connection = new TivoConnection();

            OnOperationStarted();

            try
            {
                await connection.ConnectAway(this.Username, this.Password);

                ConnectionSettings.Username = this.Username;
                ConnectionSettings.Password = this.Password;
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

            if (this.ParsedIPAddress == IPAddress.None)
            {
                return;
            }

            OnOperationStarted();

            try
            {
                await connection.Connect(this.ParsedIPAddress, this.MediaAccessKey);

                ConnectionSettings.TivoIPAddress = this.TivoIPAddress;
                ConnectionSettings.MediaAccessKey = this.MediaAccessKey;
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

        public async void SearchLAN()
        {
            this.discoveredTivos.Clear();

            var dnsAnswers = await MDnsClient.CreateAndResolveAsync(dnsProtocol);

            if (dnsAnswers != null)
            {
                var answersSubscription = dnsAnswers
                    .Select(HandleDnsAnswer)
                    .Where(x => x != null)
                    .ObserveOnDispatcher()
                    .Subscribe(x => this.discoveredTivos.Add(x.Item1, x.Item2));

                await TaskEx.Delay(TimeSpan.FromSeconds(5));

                answersSubscription.Dispose();

                if (this.discoveredTivos.Count > 0)
                {
                    this.TivoIPAddress = this.discoveredTivos.Values.First().ToString();
                }
            }
        }

        private Tuple<string, IPAddress> HandleDnsAnswer(Discovery.Message msg)
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

            foreach (var additional in msg.Additionals)
            {
                if (additional.Type == Discovery.Type.TXT)
                {
                    var txtData = (Discovery.Txt)additional.ResponseData;

                    foreach (var property in txtData.Properties)
                    {
                        Debug.WriteLine("TXT entry: {0} = {1}", property.Key, property.Value);
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

            return Tuple.Create(tivoName, tivoAddress);
        }
    }
}
