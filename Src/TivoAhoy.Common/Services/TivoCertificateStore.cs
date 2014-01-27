﻿using System;
using System.IO;
using Tivo.Connect;

namespace TivoAhoy.Common.Services
{
    internal class TivoCertificateStore : ICertificateStore
    {
        private static Lazy<ICertificateStore> instance = new Lazy<ICertificateStore>(() => new TivoCertificateStore());

        public static ICertificateStore Instance
        {
            get { return instance.Value; }
        }

        private TivoCertificateStore()
        {
        }

        Stream ICertificateStore.GetCertificate(TivoServiceProvider serviceProvider, bool fallback)
        {
            string resourceName;
            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    resourceName = fallback ? "TivoTest.tivo_us.p12" : "TivoTest.tivo_us_2013.p12";
                    break;
                case TivoServiceProvider.VirginMediaUK:
                    resourceName = fallback ? "TivoTest.tivo_vm.p12" : "TivoTest.tivo_vm_2013.p12";
                    break;

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("serviceProvider", "Must specify a valid service provider.");
            }

            return typeof(TivoCertificateStore).Assembly.GetManifestResourceStream(resourceName);
        }

        string ICertificateStore.GetPassword(TivoServiceProvider serviceProvider, bool fallback)
        {
            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    return fallback ? "mpE7Qy8cSqdf" : "l18XSexbHr";

                case TivoServiceProvider.VirginMediaUK:
                    return fallback ? "R2N48DSKr2Cm" : "AY1T2SGRktnz";

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("serviceProvider", "Must specify a valid service provider.");
            }
        }
    }
}
