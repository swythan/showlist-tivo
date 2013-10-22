using System;
using System.ComponentModel;
using System.Net;
using Caliburn.Micro;

namespace TivoAhoy.Common.Discovery
{
    public class DiscoveredTivo : INotifyPropertyChanged
    {
        private readonly string name;
        private readonly string tsn;
        private readonly string platform;
        private readonly IPAddress ipAddress;

        public DiscoveredTivo(string name, IPAddress ipAddress, string tsn, string platform)
        {
            this.name = name;
            this.ipAddress = ipAddress;
            this.tsn = tsn;
            this.platform = platform;
        }

        public string Name
        {
            get { return this.name; }
        }

        public string TSN
        {
            get { return this.tsn; }
        }

        public string Platform
        {
            get { return this.platform; }
        }

        public IPAddress IpAddress
        {
            get { return this.ipAddress; }
        }

        public bool IsVirginMedia
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.platform))
                    return true; // prev saved value would have been VM

                // VM seems to start with VM then have a four digit model number then more letters
                // VM8685DVB
                // TiVo US is tcd/Series<n> - so tcd/Series4 or tcd/Series5
                return this.platform.StartsWith("VM", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }
}
