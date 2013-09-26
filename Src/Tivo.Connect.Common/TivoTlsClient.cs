﻿//-----------------------------------------------------------------------
// <copyright file="TivoTlsClient.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Pkcs;

namespace Tivo.Connect
{
    public class TivoTlsClient : DefaultTlsClient
    {
        public TivoTlsClient()
        {
        }

        class TivoTlsAuthentication : TlsAuthentication
        {
            private readonly TlsClientContext context;

            public TivoTlsAuthentication(TlsClientContext context)
            {
                this.context = context;
            }

            public void NotifyServerCertificate(Certificate serverCertificate)
            {
            }
            
            public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
            {
                // Load PKCS12 certificate store from resources
                //var keyStreamInfo = System.Windows.Application.GetResourceStream(new Uri("tivo.p12", UriKind.Relative));
                //var keyStore = new Pkcs12Store(keyStreamInfo.Stream, "mpE7Qy8cSqdf".ToCharArray());

                // var keyStreamInfo = System.Windows.Application.GetResourceStream(new Uri("/tivo_vm.p12", UriKind.Relative));
                var keyStreamInfo = System.Windows.Application.GetResourceStream(new Uri("tivo_vm.p12", UriKind.Relative));
                var keyStore = new Pkcs12Store(keyStreamInfo.Stream, "R2N48DSKr2Cm".ToCharArray());

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
            return new TivoTlsAuthentication(this.context);
        }
    }
}
