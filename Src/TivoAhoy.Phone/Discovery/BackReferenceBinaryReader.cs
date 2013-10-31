namespace TivoAhoy.Phone.Discovery
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    class BackReferenceBinaryReader : BinaryReader
    {
        public BackReferenceBinaryReader(Stream input)
            : base(input)
        {

        }

        public BackReferenceBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding)
        {

        }

        Dictionary<int, object> registeredElements = new Dictionary<int, object>();

        public T Get<T>(int p)
        {
            return (T)registeredElements[p];
        }

        public void Register(int p, object value)
        {
            registeredElements.Add(p, value);
        }
    }
}
