using System.IO;

namespace Tivo.Connect
{
    public interface ICertificateStore
    {
        Stream GetCertificate(TivoServiceProvider serviceProvider, bool fallback);
        string GetPassword(TivoServiceProvider serviceProvider, bool fallback);
    }
}
