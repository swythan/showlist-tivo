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

        public async Task<TivoConnection> GetConnectionAsync()
        {
            if (this.connection == null)
            {
                var localConnection = new TivoConnection();

                var serviceProvider = TivoServiceProvider.VirginMediaUK;

                try
                {
                    if (this.IsAwayModeEnabled)
                    {
                        await localConnection.ConnectAway(TivoUsername, TivoPassword, serviceProvider, TivoCertificateStore.Instance);
                    }
                    else
                    {
                        await localConnection.Connect(TivoIPAddress, TivoMak, serviceProvider, TivoCertificateStore.Instance);
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
