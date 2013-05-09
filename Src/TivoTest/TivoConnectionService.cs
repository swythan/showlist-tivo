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
                        await localConnection.ConnectAway(@"james.chaldecott@virginmedia.com", @"lambBh00na");
                    }
                    else
                    {
                        await localConnection.Connect(IPAddress.Parse("192.168.0.100"), "9837127953");
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
