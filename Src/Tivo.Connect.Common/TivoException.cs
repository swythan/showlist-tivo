using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Tls;
using Tivo.Connect.Entities;

namespace Tivo.Connect
{
    public class TivoException : Exception
    {
        public TivoException(string text, string code, string operationName)
            : base(GetMessage(text, code, operationName))
        {
            this.OriginalText = text;
            this.Code = code;
            this.OperationName = operationName;
        } 

        public TivoException(string text, string code, string operationName, Exception inner)
            : base(GetMessage(text, code, operationName), inner)
        {
            this.OriginalText = text;
            this.Code = code;
            this.OperationName = operationName;
        }

        public string OriginalText { get; private set; }

        public string Code { get; private set; }

        public string OperationName { get; private set; }

        private static string GetMessage(string text, string code, string operationName)
        {
            return string.Format(
                "{0} returned an error.\n Error code: {1}\nError text:{2}",
                operationName,
                code,
                text);
        }
    }
}
