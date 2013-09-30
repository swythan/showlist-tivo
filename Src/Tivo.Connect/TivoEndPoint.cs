using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tivo.Connect
{
    public class TivoEndPoint
    {
        public string Address { get; private set; }
        public TivoMode Mode { get; private set; }
        public Stream Certificate { get; private set; }
        public string Password { get; private set; }
        public bool IsVirginMedia { get; private set; }

        public TivoEndPoint(string address, TivoMode mode, Stream certificate, string password, bool isVirginMedia)
        {
            if (certificate == null) 
                throw new ArgumentNullException("certificate");
            if (password == null) 
                throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException("address");
            
            Address = address;
            Mode = mode;
            Certificate = certificate;
            Password = password;
            IsVirginMedia = isVirginMedia;
        }
    }

    public enum TivoMode
    {
        Away = 443,
        Local = 1413
    }
}
