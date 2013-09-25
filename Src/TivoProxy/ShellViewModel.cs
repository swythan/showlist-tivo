﻿//-----------------------------------------------------------------------
// <copyright file="ShellViewModel.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Caliburn.Micro;
using ZeroconfService;

namespace TivoProxy
{
    public class ShellViewModel : Screen, IShell
    {
        private const string TivoFriendlyName = "EFFE";
        private const string TivoTSN = "CF0010E3397EFFE";
        private const string LocalIPAddress = @"192.168.0.12";
        private const string DefaultTivoIPAddress = "192.168.0.100";

        private string tivoIPAddress = DefaultTivoIPAddress;

        private X509Certificate serverCertificate;

        private IDisposable serverSubscription;

        private NetService mindService;
        private NetService remoteService;
        private NetService httpService;
        private NetService videostreamService;

        public ShellViewModel()
        {
            var certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            certStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var serverCert = certStore.Certificates
                .OfType<X509Certificate2>()
                .FirstOrDefault(x => x.Subject == "CN=secure-tivo-api.virginmedia.com");

            this.serverCertificate = serverCert; // new X509Certificate("TivoTest.pfx", "TivoTest");
        }

        public string TivoIPAddress
        {
            get { return this.tivoIPAddress; }
            set
            {
                if (this.tivoIPAddress == value)
                {
                    return;
                }

                this.tivoIPAddress = value;
                NotifyOfPropertyChange(() => this.TivoIPAddress);
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

                IPAddress address;
                return IPAddress.TryParse(this.tivoIPAddress, out address);
            }
        }

        public void Start()
        {
            StartListeningLan();
            StartListeningAway();
            AdvertiseService(TivoFriendlyName, TivoTSN);

            NotifyOfPropertyChange(() => this.CanStart);
            NotifyOfPropertyChange(() => this.CanStop);
        }

        private void StartListeningLan()
        {
            this.serverSubscription = ObservableTcpListener.Start(IPAddress.Parse(LocalIPAddress), 1413, 1)
                .Subscribe(OnLanClientConnected);
        }

        private void OnLanClientConnected(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false);

            // Authenticate the server but don't require the client to authenticate. 
            try
            {
                sslStream.AuthenticateAsServer(this.serverCertificate, false, SslProtocols.Default, false);

                // Display the properties and settings for t5he authenticated stream.
                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);

                // Set timeouts for the read and write to 30 seconds.
                sslStream.ReadTimeout = 30000;
                sslStream.WriteTimeout = 30000;

                var proxy = new ProxyConnection(sslStream, IPAddress.Parse(this.TivoIPAddress));
            }
            //catch (AuthenticationException e)
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
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
            this.serverSubscription = ObservableTcpListener.Start(IPAddress.Parse(LocalIPAddress), 443, 1)
                .Subscribe(OnAwayClientConnected);
        }

        private void OnAwayClientConnected(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream(), false);

            // Authenticate the server but don't require the client to authenticate. 
            try
            {
                sslStream.AuthenticateAsServer(this.serverCertificate, false, SslProtocols.Default, false);

                // Display the properties and settings for t5he authenticated stream.
                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);

                // Set timeouts for the read and write to 30 seconds.
                sslStream.ReadTimeout = 30000;
                sslStream.WriteTimeout = 30000;

                var proxy = new ProxyConnection(sslStream);
            }
            //catch (AuthenticationException e)
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
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

        static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }

        static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }

        static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }

        static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }

        private static void DisplayUsage()
        {
            Console.WriteLine("To start the server specify:");
            Console.WriteLine("serverSync certificateFile.cer");
            Environment.Exit(1);
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

                return false;
            }
        }

        public void Stop()
        {
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
