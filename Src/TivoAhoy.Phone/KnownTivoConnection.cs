using System.Net;
using System.Runtime.Serialization;

namespace TivoAhoy.Phone
{
    [DataContract]
    public class KnownTivoConnection
    {
        [DataMember]
        public string TSN { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string MediaAccessKey { get; set; }

        [DataMember]
        public string IpAddress { get; set; }

        public IPAddress LastIpAddress
        {
            get
            {
                IPAddress result;
                if (IPAddress.TryParse(this.IpAddress, out result))
                {
                    return result;
                }

                return null;
            }

            set
            {
                if (value == null)
                {
                    this.IpAddress = null;
                }
                else
                {
                    this.IpAddress = value.ToString();
                }
            }
        }
    }
}
