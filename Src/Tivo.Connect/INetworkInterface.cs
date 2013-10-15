using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tivo.Connect
{
    public interface INetworkInterface : IDisposable
    {
        Task<Stream> Initialize(TivoEndPoint endPoint);
    }
}
