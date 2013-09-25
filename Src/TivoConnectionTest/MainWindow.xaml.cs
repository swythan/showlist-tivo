//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TivoAhoy.Common.Discovery;

namespace TivoConnectionTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string dnsProtocol = "_tivo-mindrpc._tcp.";

        private ObservableCollection<DiscoveredTivo> discoveredTivos = new ObservableCollection<DiscoveredTivo>();
        private StringBuilder debugText = new StringBuilder();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.SearchLAN();
        }
        public ObservableCollection<DiscoveredTivo> DiscoveredTivos
        {
            get { return this.discoveredTivos; }
        }

        public async void SearchLAN()
        {
            this.discoveredTivos.Clear();

            try
            {
                AddDebugText("Searching...");
                AddDebugText("");

                Mouse.OverrideCursor = Cursors.Wait;

                var dnsAnswers = await MDnsClient.CreateAndResolveAsync(dnsProtocol);

                if (dnsAnswers != null)
                {
                    var answersSubscription = dnsAnswers
                        .Select(HandleDnsAnswer)
                        .Where(x => x != null)
                        .ObserveOnDispatcher()
                        .Subscribe(x => this.discoveredTivos.Add(x));

                    await Task.Delay(TimeSpan.FromSeconds(3));

                    answersSubscription.Dispose();
                }
            }
            finally
            {
                AddDebugText("Search finished");
                
                Mouse.OverrideCursor = null;
            }
        }

        private DiscoveredTivo HandleDnsAnswer(TivoAhoy.Common.Discovery.Message msg)
        {
            bool isTivoResponse = false;

            foreach (var answer in msg.Answers)
            {
                if (answer.Type == TivoAhoy.Common.Discovery.Type.PTR)
                {
                    var ptrData = (TivoAhoy.Common.Discovery.Ptr)answer.ResponseData;
                    var name = ptrData.DomainName.ToString();

                    if (name.Contains(dnsProtocol))
                    {
                        isTivoResponse = true;
                    }
                }
            }

            if (!isTivoResponse)
            {
                AddDebugText("Non-TiVo response received: {0}", msg);
                return null;
            }

            int? port = null;
            IPAddress tivoAddress = null;
            string tivoName = null;
            string tivoTsn = null;

            foreach (var additional in msg.Additionals)
            {
                if (additional.Type == TivoAhoy.Common.Discovery.Type.TXT)
                {
                    var txtData = (TivoAhoy.Common.Discovery.Txt)additional.ResponseData;

                    foreach (var property in txtData.Properties)
                    {
                        AddDebugText("TXT entry: {0} = {1}", property.Key, property.Value);
                        if (property.Key == "TSN")
                        {
                            tivoTsn = property.Value;
                        }
                    }
                }

                if (additional.Type == TivoAhoy.Common.Discovery.Type.A)
                {
                    var hostAddress = (TivoAhoy.Common.Discovery.HostAddress)additional.ResponseData;

                    tivoAddress = hostAddress.Address;
                    tivoName = additional.DomainName[0];
                }

                if (additional.Type == TivoAhoy.Common.Discovery.Type.SRV)
                {
                    var srvData = (TivoAhoy.Common.Discovery.Srv)additional.ResponseData;

                    port = srvData.Port;
                }
            }

            AddDebugText("TiVo found at {0}:{1}", msg.From.Address, port);

            return new DiscoveredTivo(tivoName, tivoAddress, tivoTsn);
        }

        private void AddDebugText(string format, params object[] args)
        {
            debugText.AppendLine(string.Format(format, args));

            Dispatcher.BeginInvoke(
                (Action)(() =>
                {
                    string newTextValue = this.debugText.ToString();

                    if (this.textBox.Text.Length != newTextValue.Length)
                    {
                        this.textBox.Text = newTextValue;

                        this.textBox.Focus();
                        this.textBox.CaretIndex = newTextValue.Length;
                        this.textBox.ScrollToEnd();
                    }
                }));
        }
    }
}
