using System.IO;

using Org.BouncyCastle.Asn1.Utilities;

namespace Org.BouncyCastle.Bcpg.OpenPgp
{
    public class WrappedGeneratorStream
        : FilterStream
    {
        private readonly IStreamGenerator gen;

        public WrappedGeneratorStream(
            IStreamGenerator gen,
            Stream str)
            : base(str)
        {
            this.gen = gen;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gen.Close();
            }
        }
    }
}
