//-----------------------------------------------------------------------
// <copyright file="ShellViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ARSoft.Tools.Net.Dns;
using Caliburn.Micro;
using Tivo.Connect;
using TivoProxy.Properties;
using ZeroconfService;

namespace TivoProxy
{
    public class ShellViewModel : Screen, IShell
    {
        private X509Certificate serverCertificate;

        private IDisposable serverSubscription;

        private DnsServer dnsServer;
        private NetService mindService;
        private NetService remoteService;
        private NetService httpService;
        private NetService videostreamService;
        private Tuple<NetworkInterface, IPAddress> currentNetworkInterface;

        public ShellViewModel()
        {
        }

        private void LoadServerCertificate()
        {
            //var certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            //certStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            //var serverCert = certStore.Certificates
            //    .OfType<X509Certificate2>()
            //    .FirstOrDefault(x => x.Subject == "CN=secure-tivo-api.virginmedia.com");

            if (this.IsVirgin)
            {
                this.serverCertificate = new X509Certificate2("TivoTest_vm.pfx", "p@ssw0rd");
            }
            else
            {
                this.serverCertificate = new X509Certificate2("TivoTest_us.pfx", "p@ssw0rd");
            }
        }

        public bool IsVirgin
        {
            get { return Settings.Default.IsVirgin; }
            set
            {
                if (Settings.Default.IsVirgin == value)
                {
                    return;
                }

                Settings.Default.IsVirgin = value;
                NotifyOfPropertyChange(() => this.IsVirgin);
            }
        }

        public string TivoIPAddress
        {
            get { return Settings.Default.TivoIPAddress; }
            set
            {
                if (Settings.Default.TivoIPAddress == value)
                {
                    return;
                }

                Settings.Default.TivoIPAddress = value;
                NotifyOfPropertyChange(() => this.TivoIPAddress);
                NotifyOfPropertyChange(() => this.CanStart);
            }
        }

        public string Tsn
        {
            get { return Settings.Default.TivoTsn; }
            set
            {
                if (Settings.Default.TivoTsn == value)
                {
                    return;
                }

                Settings.Default.TivoTsn = value;
                NotifyOfPropertyChange(() => this.Tsn);
                NotifyOfPropertyChange(() => this.CanStart);
            }
        }

        public string FriendlyName
        {
            get { return Settings.Default.TivoFriendlyName; }
            set
            {
                if (Settings.Default.TivoFriendlyName == value)
                {
                    return;
                }

                Settings.Default.TivoFriendlyName = value;
                NotifyOfPropertyChange(() => this.FriendlyName);
                NotifyOfPropertyChange(() => this.CanStart);
            }
        }

        public IEnumerable<Tuple<NetworkInterface, IPAddress>> NetworkInterfaces
        {
            get
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                  .Where(x => x.Supports(NetworkInterfaceComponent.IPv4) && x.OperationalStatus == OperationalStatus.Up)
                  .Select(x => Tuple.Create(x, this.GetIPv4Address(x)));
            }
        }

        public Tuple<NetworkInterface, IPAddress> CurrentNetworkInterface
        {
            get { return this.currentNetworkInterface; }
            set
            {
                if (this.currentNetworkInterface == value)
                {
                    return;
                }

                this.currentNetworkInterface = value;
                NotifyOfPropertyChange(() => this.CurrentNetworkInterface);
                NotifyOfPropertyChange(() => this.CanStart);
            }
        }
        public bool CanStart
        {
            get
            {
                if (this.mindService != null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Tsn) ||
                    string.IsNullOrWhiteSpace(this.FriendlyName) ||
                    this.CurrentNetworkInterface == null)
                {
                    return false;
                }

                IPAddress address;
                return IPAddress.TryParse(this.TivoIPAddress, out address);
            }
        }

        public void Start()
        {
            this.LoadServerCertificate();

            StartDnsProxy();

            StartListeningLan();
            StartListeningAway();
            AdvertiseService(this.FriendlyName, this.Tsn);

            Settings.Default.Save();

            NotifyOfPropertyChange(() => this.CanStart);
            NotifyOfPropertyChange(() => this.CanStop);
        }

        private void StartDnsProxy()
        {
            this.dnsServer = new DnsServer(this.CurrentNetworkInterface.Item2, 10, 10, this.ProcessDnsQuery);
            this.dnsServer.Start();
        }

        private DnsMessageBase ProcessDnsQuery(DnsMessageBase message, IPAddress clientAddress, ProtocolType protocol)
        {
            message.IsQuery = false;

            DnsMessage query = message as DnsMessage;

            if ((query != null) &&
                (query.Questions.Count == 1))
            {
                DnsQuestion question = query.Questions[0];

                // If the request is for the Away Mode server, then return our own IP address
                if (question.RecordType == RecordType.A)
                {
                    if (question.Name == "secure-tivo-api.virginmedia.com" ||
                        question.Name == "middlemind.tivo.com")
                    {
                        query.AnswerRecords.Add(new ARecord(question.Name, 60, this.CurrentNetworkInterface.Item2));
                        query.ReturnCode = ReturnCode.NoError;

                        return query;
                    }
                }

                // send query to upstream server
                DnsMessage answer = DnsClient.Default.Resolve(question.Name, question.RecordType, question.RecordClass);

                // if got an answer, copy it to the message sent to the client
                if (answer != null)
                {
                    foreach (DnsRecordBase record in (answer.AnswerRecords))
                    {
                        query.AnswerRecords.Add(record);
                    }
                    foreach (DnsRecordBase record in (answer.AdditionalRecords))
                    {
                        query.AnswerRecords.Add(record);
                    }

                    query.ReturnCode = ReturnCode.NoError;
                    return query;
                }
            }

            // Not a valid query or upstream server did not answer correct
            message.ReturnCode = ReturnCode.ServerFailure;
            return message;
        }

        private void StartListeningLan()
        {
            this.serverSubscription = ObservableTcpListener.Start(this.CurrentNetworkInterface.Item2, 1413, 1)
                .Subscribe(OnLanClientConnected);
        }

        private IPAddress GetIPv4Address(NetworkInterface adapter)
        {
            var localAddress = adapter.GetIPProperties()
                .UnicastAddresses
                .Select(x => x.Address)
                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

            return localAddress;
        }

        private void OnLanClientConnected(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false);

            // Authenticate the server but don't require the client to authenticate. 
            try
            {
                sslStream.AuthenticateAsServer(this.serverCertificate, false, SslProtocols.Default, false);

                TivoProxyEventSource.Log.ClientConnected(
                    TivoConnectionMode.Local,
                    ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(),
                    FormatStreamProperties(sslStream));

                // Set timeouts for the read and write to 30 seconds.
                sslStream.ReadTimeout = 30000;
                sslStream.WriteTimeout = 30000;

                var serviceProvider = this.IsVirgin ? TivoServiceProvider.VirginMediaUK : TivoServiceProvider.TivoUSA;

                var serverEndPoint = TivoEndPoint.CreateLocal(this.TivoIPAddress, serviceProvider, TivoCertificateStore.Instance);

                var proxy = new ProxyConnection(sslStream, serverEndPoint);
            }
            //catch (AuthenticationException e)
            catch (Exception e)
            {
                TivoProxyEventSource.Log.ClientConnectionFailure(TivoConnectionMode.Local,e);

                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                //// The client stream will be closed with the sslStream 
                //// because we specified this behavior when creating 
                //// the sslStream.
                //sslStream.Close();
                //client.Close();
            }
        }

        private void StartListeningAway()
        {
            this.serverSubscription = ObservableTcpListener.Start(this.CurrentNetworkInterface.Item2, 443, 1)
                .Subscribe(OnAwayClientConnected);
        }

        private void OnAwayClientConnected(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false);

            // Authenticate the server but don't require the client to authenticate. 
            try
            {
                sslStream.AuthenticateAsServer(this.serverCertificate, false, SslProtocols.Default, false);

                TivoProxyEventSource.Log.ClientConnected(
                    TivoConnectionMode.Away, 
                    ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(), 
                    FormatStreamProperties(sslStream));

                // Set timeouts for the read and write to 30 seconds.
                sslStream.ReadTimeout = 30000;
                sslStream.WriteTimeout = 30000;

                var serviceProvider = this.IsVirgin ? TivoServiceProvider.VirginMediaUK : TivoServiceProvider.TivoUSA;

                var serverEndPoint = TivoEndPoint.CreateAway(serviceProvider, TivoCertificateStore.Instance);
                
                var proxy = new ProxyConnection(sslStream, serverEndPoint);
            }
            //catch (AuthenticationException e)
            catch (Exception e)
            {
                TivoProxyEventSource.Log.ClientConnectionFailure(TivoConnectionMode.Local, e);

                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                //// The client stream will be closed with the sslStream 
                //// because we specified this behavior when creating 
                //// the sslStream.
                //sslStream.Close();
                //client.Close();
            }
        }

        private string FormatStreamProperties(SslStream sslStream)
        {
            var result = new StringBuilder();

            DisplaySecurityLevel(sslStream, result);
            DisplaySecurityServices(sslStream, result);
            DisplayCertificateInformation(sslStream, result);
            DisplayStreamProperties(sslStream, result);

            return result.ToString();
        }

        static void DisplaySecurityLevel(SslStream stream, StringBuilder result)
        {
            result.AppendFormat("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength).AppendLine();
            result.AppendFormat("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength).AppendLine();
            result.AppendFormat("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength).AppendLine();
            result.AppendFormat("Protocol: {0}", stream.SslProtocol).AppendLine();
        }

        static void DisplaySecurityServices(SslStream stream, StringBuilder result)
        {
            result.AppendFormat("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer).AppendLine();
            result.AppendFormat("IsSigned: {0}", stream.IsSigned).AppendLine();
            result.AppendFormat("Is Encrypted: {0}", stream.IsEncrypted).AppendLine();
        }

        static void DisplayStreamProperties(SslStream stream, StringBuilder result)
        {
            result.AppendFormat("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite).AppendLine();
            result.AppendFormat("Can timeout: {0}", stream.CanTimeout).AppendLine();
        }

        static void DisplayCertificateInformation(SslStream stream, StringBuilder result)
        {
            result.AppendFormat("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus).AppendLine();

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                result.AppendFormat("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString())
                    .AppendLine();
            }
            else
            {
                result.AppendFormat("Local certificate is null.").AppendLine();
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                result.AppendFormat("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString())
                    .AppendLine();
            }
            else
            {
                result.AppendFormat("Remote certificate is null.").AppendLine();
            }
        }

        private void AdvertiseService(string name, string tsn)
        {
            this.deviceService = SetupMdnsService(
                name,
                "_tivo-device._tcp.",
                80,
                new Dictionary<string, string>()
                {
                    {"path", "/"},
                    {"platform", "VM8685DVB"},
                    {"platformName", "Virgin Media TiVo DVR"},
                    {"swversion", "15.3.1.RC3-VMC-2-C00"},
                    {"services", "_tivo-mindrpc._tcp,_tivo-remote._tcp"},
                    {"TSN", tsn},            
                });

            this.remoteService = SetupMdnsService(
                name,
                "_tivo-remote._tcp.",
                31339,
                new Dictionary<string, string>()
                {
                    {"path", "/"},
                    {"platform", "VM8685DVB"},
                    {"protocol", "tivo-remote"},
                    {"swversion", "15.3.1.RC3-VMC-2-C00"},
                    {"TSN", tsn},            
                });

            this.mindService = SetupMdnsService(
                name,
                "_tivo-mindrpc._tcp.",
                1413,
                new Dictionary<string, string>()
                {
                    {"path", "/"},
                    {"platform", "VM8685DVB"},
                    {"protocol", "tivo-mindrpc"},
                    {"swversion", "15.3.1.RC3-VMC-2-C00"},
                    {"TSN", tsn},            
                });

            this.videostreamService = SetupMdnsService(
                name,
                "_tivo-videostream._tcp.",
                80,
                new Dictionary<string, string>()
                {
                    {"path", "/TiVoConnect?Command=QueryContainer&Container=%2FNowPlaying"},
                    {"platform", "VM8685DVB"},
                    {"protocol", "https"},
                    {"swversion", "15.3.1.RC3-VMC-2-C00"},
                    {"TSN", tsn},            
                });

            this.httpService = SetupMdnsService(
                name,
                "_http._tcp.",
                80,
                new Dictionary<string, string>()
                {
                    {"path", "/index.html"},
                    {"platform", "VM8685DVB"},
                    {"swversion", "15.3.1.RC3-VMC-2-C00"},
                    {"TSN", tsn},            
                });

            this.deviceService.Publish();
            this.remoteService.Publish();
            this.mindService.Publish();
            this.videostreamService.Publish();
            this.httpService.Publish();
        }

        private NetService SetupMdnsService(string name, string serviceType, int port, Dictionary<string, string> txt)
        {
            var service = new NetService("local.", serviceType, name, port);
            service.TXTRecordData = NetService.DataFromTXTRecordDictionary(txt);

            service.AllowMultithreadedCallbacks = true;
            service.DidPublishService += service_DidPublishService;
            service.DidNotPublishService += service_DidNotPublishService;
            service.DidUpdateTXT += service_DidUpdateTXT;

            return service;
        }

        void service_DidPublishService(NetService service)
        {
        }

        void service_DidNotPublishService(NetService service, DNSServiceException exception)
        {
        }

        void service_DidUpdateTXT(NetService service)
        {
        }

        public bool CanStop
        {
            get
            {
                if (this.serverSubscription != null)
                {
                    return true;
                }

                if (this.mindService != null)
                {
                    return true;
                }

                if (this.dnsServer != null)
                {
                    return true;
                }

                return false;
            }
        }

        public void Stop()
        {
            if (this.dnsServer != null)
            {
                this.dnsServer.Stop();
                this.dnsServer = null;
            }

            if (this.serverSubscription != null)
            {
                this.serverSubscription.Dispose();
                this.serverSubscription = null;
            }

            if (this.deviceService != null)
            {
                this.deviceService.Dispose();
                this.deviceService = null;
            }

            if (this.remoteService != null)
            {
                this.remoteService.Dispose();
                this.remoteService = null;
            }

            if (this.mindService != null)
            {
                this.mindService.Dispose();
                this.mindService = null;
            }

            if (this.httpService != null)
            {
                this.httpService.Dispose();
                this.httpService = null;
            }

            if (this.videostreamService != null)
            {
                this.videostreamService.Dispose();
                this.videostreamService = null;
            }

            NotifyOfPropertyChange(() => this.CanStart);
            NotifyOfPropertyChange(() => this.CanStop);
        }

        public NetService deviceService { get; set; }
    }
}
