namespace TivoAhoy.Common.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class BinaryHelper
    {
        public static void Write(Stream stream, ushort p)
        {
            Write(stream, ToBytes(p));
        }

        public static byte[] GetBytes(IServerResponse response)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                response.WriteTo(stream);
                return stream.ToArray();
            }
        }

        public static byte[] GetBytes(IClientRequest response)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                response.WriteTo(stream);
                return stream.ToArray();
            }
        }

        public static void Write(Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(Stream stream, uint value)
        {
            Write(stream, BitConverter.GetBytes(value));
        }

        public static ushort ReadUInt16(BinaryReader reader)
        {
            ushort result;
            FromBytes(reader.ReadBytes(2), out result);
            return result;
        }

        public static uint ReadUInt32(BinaryReader reader)
        {
            uint result;
            FromBytes(reader.ReadBytes(4), out result);
            return result;
        }

        public static byte[] ToBytes(ushort s)
        {
            byte[] bytes = new byte[2];
            bytes[1] = (byte)(s % (byte.MaxValue + 1));
            s = (ushort)(s >> 8);
            bytes[0] = (byte)(s % (byte.MaxValue + 1));
            return bytes;
        }

        public static long LongFromBytes(byte[] bytes, int offset, int length)
        {
            long result = 0;
            for (int i = offset + length - 1; i >= offset; i--)
            {
                result += bytes[i] << (length - 1 - i + offset) * 8;
            }
            return result;
        }

        public static void FromBytes(byte[] bytes, int offset, out ushort s)
        {
            s = (ushort)LongFromBytes(bytes, offset, 2);
        }

        public static void FromBytes(byte[] bytes, out ushort s)
        {
            FromBytes(bytes, 0, out s);
        }

        public static void FromBytes(byte[] bytes, out uint i)
        {
            FromBytes(bytes, 0, out i);
        }

        public static void FromBytes(byte[] bytes, int offset, out uint i)
        {
            i = (uint)LongFromBytes(bytes, offset, 4);
        }

        public static T FromBytes<T>(IServerRequest<T> response, byte[] responseBytes)
        {
            using (MemoryStream ms = new MemoryStream(responseBytes))
            {
                return response.GetRequest(ms);
            }
        }
    }
}
