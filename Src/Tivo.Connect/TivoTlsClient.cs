//-----------------------------------------------------------------------
// <copyright file="TivoTlsClient.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Pkcs;

namespace Tivo.Connect
{
    public class TivoTlsClient : DefaultTlsClient
    {
        private readonly Stream _certificate;
        private readonly string _password;

        public TivoTlsClient(Stream certificate, string password)
        {
            if (certificate == null) 
                throw new ArgumentNullException("certificate");
            if (password == null) 
                throw new ArgumentNullException("password");

            _certificate = certificate;
            _password = password;
        }

        class TivoTlsAuthentication : TlsAuthentication
        {
            private readonly TivoTlsClient _context;

            public TivoTlsAuthentication(TivoTlsClient context)
            {
                _context = context;
            }

            public void NotifyServerCertificate(Certificate serverCertificate)
            {
            }
            
            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                // Load PKCS12 certificate store from stream resources
                var keyStore = new Pkcs12Store(_context._certificate, _context._password.ToCharArray());

                // Convert keys into structures needed for Certificate constructor
                var aliases = keyStore.Aliases.OfType<string>();

                var certStructures = aliases
                    .SelectMany(keyStore.GetCertificateChain)
                    .Select(x => x.Certificate)
                    .Distinct()
                    .Select(x => x.CertificateStructure)
                    .ToArray();

                // Get the private key
                var keyEntry = keyStore.GetKey(aliases.First());

                return new DefaultTlsSignerCredentials(_context.context, new Certificate(certStructures), keyEntry.Key);
            }
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new TivoTlsAuthentication(this);
        }
    }
}
