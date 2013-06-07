using System.ComponentModel;
using System.Net;
using Caliburn.Micro;

namespace TivoAhoy.Common.Discovery
{
    public class DiscoveredTivo : INotifyPropertyChanged
    {
        private readonly string name;
        private readonly string tsn;
        private readonly IPAddress ipAddress;

        public DiscoveredTivo(string name, IPAddress ipAddress, string tsn)
        {
            this.name = name;
            this.ipAddress = ipAddress;
            this.tsn = tsn;
        }

        public string Name
        {
            get { return this.name; }
        }

        public string TSN
        {
            get { return this.tsn; }
        }

        public IPAddress IpAddress
        {
            get { return this.ipAddress; }
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { }
            remove { }
        }
    }
}
