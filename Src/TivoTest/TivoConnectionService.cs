using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        private const string TivoUsername = @"james.chaldecott@virginmedia.com";
        private const string TivoPassword = @"lambBh00na";
        private const string TivoIPAddress = "192.168.0.100";
        private const string TivoMak = "9837127953";

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
                try
                {
                    if (this.IsAwayModeEnabled)
                    {
                        await localConnection.ConnectAway(TivoUsername, TivoPassword);
                    }
                    else
                    {
                        await localConnection.Connect(IPAddress.Parse(TivoIPAddress), TivoMak);
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
