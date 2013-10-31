using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Tivo.Connect;
using Tivo.Connect.Entities;
using Zeroconf;

namespace TivoTest
{
    public class SetupConnectionViewModel : PropertyChangedBase
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        private string username;
        private string password;

        private string mediaAccessKey;

        private ObservableCollection<AnyBody> associatedTivos = new ObservableCollection<AnyBody>();

        private ObservableCollection<DiscoveredTivo> discoveredTivos = new ObservableCollection<DiscoveredTivo>();

        public SetupConnectionViewModel()
        {
        }

        public string Username
        {
            get
            {
                return this.username;
            }

            set
            {
                this.username = value;

            }
        }

        public string Password
        {
            get { return this.password; }
            set
            {
                this.password = value;

                this.NotifyOfPropertyChange(() => this.Password);
            }
        }

        public string MediaAccessKey
        {
            get { return mediaAccessKey; }
            set
            {
                mediaAccessKey = value; this.NotifyOfPropertyChange(() => this.MediaAccessKey);
            }
        }

        public ObservableCollection<AnyBody> AssociatedTivos
        {
            get
            {
                return associatedTivos;
            }
        }

        public ObservableCollection<DiscoveredTivo> DiscoveredTivos
        {
            get
            {
                return discoveredTivos;
            }
        }

        public async void ConnectAndSearch()
        {
            this.associatedTivos.Clear();
            this.discoveredTivos.Clear();

            var searchTask = DiscoverTivosAsync();
            var connectTask = ConnectAsync();

            try
            {
                await connectTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error connecting to service.\n{0}", ex), "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                await searchTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error searching for TiVo's on LAN.\n{0}", ex), "Search Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ConnectAsync()
        {
            var localConnection = new TivoConnection();
            var serviceProvider = TivoServiceProvider.VirginMediaUK;

            try
            {
                await localConnection.ConnectAway(this.Username, this.Password, serviceProvider, TivoCertificateStore.Instance);

                this.MediaAccessKey = localConnection.MediaAccessKey;
                foreach (var body in localConnection.AssociatedTivos)
                {
                    this.AssociatedTivos.Add(body);
                }
            }
            catch (Exception)
            {
                localConnection.Dispose();
                throw;
            }
        }

        private async Task<bool> DiscoverTivosAsync()
        {
            const string tivoService = "_tivo-mindrpc._tcp.local.";

            var results = await ZeroconfResolver.ResolveAsync(tivoService, TimeSpan.FromSeconds(.5));

            if (results.Count <= 0)
            {
                return false;
            }

            foreach (var host in results)
            {
                var device = new DiscoveredTivo(host.DisplayName,
                    IPAddress.Parse(host.IPAddress),
                    host.Services.First().Value.Properties[0]["TSN"]);

                this.DiscoveredTivos.Add(device);
            }

            return true;
        }

    }
}
