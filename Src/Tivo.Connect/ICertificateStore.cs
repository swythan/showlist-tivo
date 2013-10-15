using System.IO;

namespace Tivo.Connect
{
    public interface ICertificateStore
    {
        Stream GetCertificate(TivoServiceProvider serviceProvider);
        string GetPassword(TivoServiceProvider serviceProvider);
    }
}
