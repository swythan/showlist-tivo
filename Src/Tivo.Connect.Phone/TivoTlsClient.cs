using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Org.BouncyCastle.Crypto.Tls;

namespace Tivo.Connect
{
    internal class TivoTlsClient : DefaultTlsClient
    {
        class AcceptAllTlsAuthentication : TlsAuthentication
        {

            public void NotifyServerCertificate(Certificate serverCertificate)
            {
                
            }

            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                return null;
            }
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new AcceptAllTlsAuthentication();
        }
    }
}
