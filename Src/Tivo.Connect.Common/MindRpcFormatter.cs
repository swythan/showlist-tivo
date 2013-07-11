using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tivo.Connect
{
    public static class MindRpcFormatter
    {
        public static byte[] EncodeRequest(bool awayMode, int sessionId, string tsn, int rpcId, string requestType, string bodyText)
        {
            // We want version 10 in Away mode (so that we get the MAK in bodyAuthenticateResponse).
            // Unfortunately using version 10 direct to a TiVo will crash it!
            int schemaVersion = awayMode ? 10 : 9;
            string headerText = CreateHeader(sessionId, tsn, rpcId, requestType, schemaVersion);

            return EncodeMessage(headerText, bodyText);
        }

        public static byte[] EncodeMessage(string headerText, string bodyText)
        {
            var messageString = string.Format("MRPC/2 {0} {1}\r\n{2}{3}",
                Encoding.UTF8.GetByteCount(headerText),
                Encoding.UTF8.GetByteCount(bodyText),
                headerText,
                bodyText);

            var messageBytes = Encoding.UTF8.GetBytes(messageString);

            return messageBytes;
        }

        public static string CreateHeader(int sessionId, string tsn, int rpcId, string requestType, int schemaVersion)
        {
            int appMajorVersion = 2;
            int appMinorVersion = 2;

            var header = new StringBuilder();
            header.AppendLine("Type:request");
            header.AppendLine(string.Format("RpcId:{0}", rpcId));
            header.AppendLine(string.Format("SchemaVersion:{0}", schemaVersion));
            header.AppendLine("Content-Type:application/json");
            header.AppendLine("RequestType:" + requestType);
            header.AppendLine("ResponseCount:single");

            if (!string.IsNullOrEmpty(tsn))
            {
                header.AppendLine(string.Format("BodyId:{0}", tsn));
            }

            header.AppendLine("X-ApplicationName:com.virginmedia.quicksilvervm");
            header.AppendLine(string.Format("X-ApplicationVersion:{0}.{1}", appMajorVersion, appMinorVersion));
            header.AppendLine(string.Format("X-ApplicationSessionId:0x{0:x}", sessionId));
            header.AppendLine();

            return header.ToString();
        }

        public static Tuple<string, string> ReadMessage(Stream stream)
        {
            List<byte> preambleBytes = new List<byte>(16);

            string preamble = null;

            List<byte> buffer = new List<byte>();
            bool hasCr = false;

            while (preamble == null)
            {
                int nextByte = stream.ReadByte();

                if (nextByte == -1)
                {
                    throw new IOException("EOF reached in preamble.");
                }

                if (nextByte == 13)
                {
                    hasCr = true;
                }
                else
                {
                    if (nextByte == 10 && hasCr)
                    {
                        preamble = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count);
                    }
                    else
                    {
                        if (hasCr)
                        {
                            buffer.Add(13);
                            hasCr = false;
                        }

                        buffer.Add((byte)nextByte);
                    }
                }
            }

            var preambleParts = preamble.Split(' ');

            var expectedHeaderByteCount = int.Parse(preambleParts[1]);
            var expectedBodyByteCount = int.Parse(preambleParts[2]);

            var headerBytes = new byte[expectedHeaderByteCount];
            int headerByteCount = 0;
            while (headerByteCount < expectedHeaderByteCount)
            {
                headerByteCount += stream.Read(headerBytes, headerByteCount, expectedHeaderByteCount - headerByteCount);
            }

            var header = Encoding.UTF8.GetString(headerBytes, 0, headerByteCount);

            var bodyBytes = new byte[expectedBodyByteCount];
            int bodyByteCount = 0;
            while (bodyByteCount < expectedBodyByteCount)
            {
                bodyByteCount += stream.Read(bodyBytes, bodyByteCount, expectedBodyByteCount - bodyByteCount);
            }

            string bodyText = Encoding.UTF8.GetString(bodyBytes, 0, bodyByteCount);

            return Tuple.Create(header, bodyText);
        }

        public static int GetRpcIdFromHeader(string header)
        {
            return GetValueFromHeader("RpcId", header);
        }

        public static int GetValueFromHeader(string valueName, string header)
        {
            var headerReader = new StringReader(header);

            int rpcId = 0;
            while (true)
            {
                var line = headerReader.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith(valueName, StringComparison.OrdinalIgnoreCase))
                {
                    var tokens = line.Split(':');
                    if (tokens.Length > 1)
                    {
                        if (int.TryParse(tokens[1], out rpcId))
                        {
                            return rpcId;
                        }
                    }
                }
            }

            return -1;
        }
    }
}
