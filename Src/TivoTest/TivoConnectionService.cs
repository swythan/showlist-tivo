//-----------------------------------------------------------------------
// <copyright file="TivoConnectionService.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Tivo.Connect;

namespace TivoTest
{
    [Export(typeof(ITivoConnectionService))]
    public class TivoConnectionService: PropertyChangedBase, ITivoConnectionService
    {
        private const string TivoUsername = null;
        private const string TivoPassword = null;
        private const string TivoIPAddress = null;
        private const string TivoMak = null;

        private bool isAwayModeEnabled = true;
        
        private TivoConnection connection = null;
        
        [ImportingConstructor]
        public TivoConnectionService()
        {
        }

        public bool IsConnected
        {
            get
            {
                return this.connection != null;
            }
        }

        public bool IsAwayModeEnabled
        {
            get
            {
                return this.isAwayModeEnabled;
            }

            set
            {
                if (this.isAwayModeEnabled == value)
                {
                    return;
                }

                this.connection = null;
                this.isAwayModeEnabled = value;

                this.NotifyOfPropertyChange(() => this.IsAwayModeEnabled);
                this.NotifyOfPropertyChange(() => this.IsConnected);
            }
        }

        private static Tuple<string, Stream> LoadCertificateAndPassword(bool isVirginMedia)
        {
            // Load the cert
            if (isVirginMedia)
            {
                var stream = typeof(TivoConnectionService).Assembly.GetManifestResourceStream("TivoTest.tivo_vm.p12");
                return Tuple.Create("R2N48DSKr2Cm", stream);
            }
            else
            {
                var stream = typeof(TivoConnectionService).Assembly.GetManifestResourceStream("TivoTest.tivo_us.p12");
                return Tuple.Create("mpE7Qy8cSqdf", stream);
            }
        }

        public async Task<TivoConnection> GetConnectionAsync()
        {
            if (this.connection == null)
            {
                var localConnection = new TivoConnection();

                var cert = LoadCertificateAndPassword(false);
                var middleMind = false ? @"secure-tivo-api.virginmedia.com" : "middlemind.tivo.com";
                try
                {
                    if (this.IsAwayModeEnabled)
                    {
                        await localConnection.ConnectAway(TivoUsername, TivoPassword, middleMind, false, cert.Item2, cert.Item1);
                    }
                    else
                    {
                        await localConnection.Connect(TivoIPAddress, TivoMak, cert.Item2, cert.Item1);
                    }

                    this.connection = localConnection;

                    NotifyOfPropertyChange(() => IsConnected);
                }
                catch (Exception)
                {
                    localConnection.Dispose();
                    throw;
                }
            }

            return this.connection;
        }
    }
}
