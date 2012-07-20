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
using System.IO;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Pkcs;
using System.Linq;

namespace Tivo.Connect
{
    internal class TivoTlsClient : DefaultTlsClient
    {
        private readonly Action<string> captureTsn;

        public TivoTlsClient(Action<string> captureTsn)
        {
            this.captureTsn = captureTsn;
        }

        class TivoTlsAuthentication : TlsAuthentication
        {
            private readonly TlsClientContext context;
            private readonly Action<string> captureTsn;

            public TivoTlsAuthentication(TlsClientContext context, Action<string> captureTsn)
            {
                this.context = context;
                this.captureTsn = captureTsn;
            }

            public void NotifyServerCertificate(Certificate serverCertificate)
            {
                X509CertificateStructure firstCert = serverCertificate.GetCerts()[0];
                X509Name subject = firstCert.Subject;

                this.captureTsn((string)subject.GetValueList()[0]);
            }
            
            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                // Load PKCS12 certificate store from resources
                var keyStreamInfo = System.Windows.Application.GetResourceStream(new Uri("tivo.p12", UriKind.Relative));
                var keyStore = new Pkcs12Store(keyStreamInfo.Stream, "mpE7Qy8cSqdf".ToCharArray());
                    
                // Convert keys into structures needed for Certificate constructor
                var aliases = keyStore.Aliases.OfType<string>();

                var certStructures = aliases
                    .SelectMany(x => keyStore.GetCertificateChain(x))
                    .Select(x => x.Certificate)
                    .Distinct()
                    .Select(x => x.CertificateStructure)
                    .ToArray();

                // Get the private key
                var keyEntry = keyStore.GetKey(aliases.First());

                return new DefaultTlsSignerCredentials(this.context, new Certificate(certStructures), keyEntry.Key);
            }
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new TivoTlsAuthentication(this.context, this.captureTsn);
        }
    }
}
