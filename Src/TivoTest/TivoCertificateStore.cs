using System;
using System.IO;
using Tivo.Connect;

namespace TivoTest
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

        Stream ICertificateStore.GetCertificate(TivoServiceProvider serviceProvider)
        {
            string resourceName;
            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    resourceName = "TivoTest.tivo_us.p12";
                    break;
                case TivoServiceProvider.VirginMediaUK:
                    resourceName = "TivoTest.tivo_vm.p12";
                    break;

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("serviceProvider", "Must specify a valid service provider.");
            }

            return typeof(TivoCertificateStore).Assembly.GetManifestResourceStream(resourceName);
        }

        string ICertificateStore.GetPassword(TivoServiceProvider serviceProvider)
        {
            switch (serviceProvider)
            {
                case TivoServiceProvider.TivoUSA:
                    return "l18XSexbHr";

                case TivoServiceProvider.VirginMediaUK:
                    return "R2N48DSKr2Cm";

                case TivoServiceProvider.Unknown:
                default:
                    throw new ArgumentOutOfRangeException("serviceProvider", "Must specify a valid service provider.");
            }
        }
    }
}
