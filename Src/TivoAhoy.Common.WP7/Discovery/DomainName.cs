namespace TivoAhoy.Common.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class DomainName : List<string>, IClientRequest
    {
        public DomainName() { }

        public DomainName(IEnumerable<string> domainName) :
            base(domainName)
        {
        }

        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            foreach (string label in this)
            {
                bytes.Add((byte)Encoding.UTF8.GetByteCount(label));
                if (label.Length == 0)
                    break;
                bytes.AddRange(Encoding.UTF8.GetBytes(label));
            }
            return bytes.ToArray();
        }

        public ushort GetByteCount()
        {
            ushort result = 1;
            foreach (string label in this)
            {
                if (label.Length == 0)
                    break;
                int bytes = Encoding.UTF8.GetByteCount(label);
                result += (ushort)(bytes + 1);
            }
            return result;
        }

        public static DomainName FromBytes(byte[] bytes, ref int index)
        {
            if (bytes[index] >> 6 == 3)
            {
                //In case of pointer
                ushort ptr;
                bytes[index] -= 3 << 6;
                BinaryHelper.FromBytes(bytes, index, out ptr);
                bytes[index] += 3 << 6;
                index += 2;
                ptr = (ushort)(ptr << 2 >> 2);
                int iPtr = ptr;
                return FromBytes(bytes, ref iPtr);
            }
            else
            {
                DomainName dn = new DomainName();

                if (bytes[index] != 0)
                {
                    dn.Add(Encoding.UTF8.GetString(bytes, index + 1, bytes[index]));
                    index += bytes[index] + 1;
                    dn.AddRange(DomainName.FromBytes(bytes, ref index));
                }
                else
                    index++;
                return dn;
            }
        }

        public override string ToString()
        {
            return string.Join(".", ToArray());
        }

        public static implicit operator string(DomainName dn)
        {
            return dn.ToString();
        }

        public static implicit operator DomainName(string s)
        {
            DomainName dn = new DomainName();
            dn.AddRange(s.Split('.'));
            return dn;
        }

        #region IResponse Members

        public void WriteTo(Stream stream)
        {
            foreach (string label in this)
            {
                if (label.Length == 0)
                    break;
                byte[] bytes = Encoding.UTF8.GetBytes(label);
                //labels.Add(new KeyValuePair<byte[], byte>(bytes, (byte)bytes.Length));
                //totalLength += (ushort)(bytes.Length + 1);
                stream.WriteByte((byte)bytes.Length);
                BinaryHelper.Write(stream, bytes);
            }
            stream.WriteByte(0);
        }
        public void WriteTo(BinaryWriter writer)
        {
            //ushort totalLength = 0;
            //List<KeyValuePair<byte[], byte>> labels = new List<KeyValuePair<byte[], byte>>();
            foreach (string label in this)
            {
                if (label.Length == 0)
                    break;
                byte[] bytes = Encoding.UTF8.GetBytes(label);
                //labels.Add(new KeyValuePair<byte[], byte>(bytes, (byte)bytes.Length));
                //totalLength += (ushort)(bytes.Length + 1);
                writer.Write((byte)bytes.Length);
                writer.Write(bytes);
            }
            writer.Write((byte)0);
            //writer.Write(totalLength);
            //foreach (var label in labels)
            //{
            //    writer.Write(label.Value);
            //    writer.Write(label.Key);
            //}
        }

        public byte[] GetBytes()
        {
            return BinaryHelper.GetBytes(this);
        }

        #endregion

        internal static DomainName Get(BackReferenceBinaryReader reader)
        {
            byte stringLength = reader.ReadByte();
            if (stringLength >> 6 == 3)
            {
                //In case of pointer
                ushort ptr;
                BinaryHelper.FromBytes(new byte[] { (byte)(stringLength - (3 << 6)), reader.ReadByte() }, out ptr);

                return reader.Get<DomainName>(ptr);
            }
            else
            {
                DomainName dn = new DomainName();

                reader.Register((int)reader.BaseStream.Position - 1, dn);

                //stringLength = reader.ReadByte();
                if (stringLength != 0)
                {
                    dn.Add(Encoding.UTF8.GetString(reader.ReadBytes(stringLength), 0, stringLength));
                    //dn.Add(Encoding.UTF8.GetString(bytes, index + 1, bytes[index]));

                    dn.AddRange(DomainName.Get(reader));
                }
                //else
                //    index++;
                return dn;
            }
        }
    }
}
