//-----------------------------------------------------------------------
// <copyright file="TivoException.cs" company="James Chaldecott">
// Copyright (c) 2012-2013 James Chaldecott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tivo.Connect
{
    public class TivoException : Exception
    {
        public TivoException(string text, string code, string cause, string operationName)
            : base(GetMessage(text, code, cause, operationName))
        {
            this.OriginalText = text;
            this.Code = code;
            this.Cause = cause;
            this.OperationName = operationName;
        } 

        public TivoException(string text, string code, string cause, string operationName, Exception inner)
            : base(GetMessage(text, code, cause, operationName), inner)
        {
            this.OriginalText = text;
            this.Code = code;
            this.OperationName = operationName;
        }

        public string OriginalText { get; private set; }

        public string Code { get; private set; }

        public string OperationName { get; private set; }
        
        public string Cause { get; private set; }

        private static string GetMessage(string text, string code, string cause, string operationName)
        {
            if (string.IsNullOrWhiteSpace(cause))
            {
                return string.Format(
                    "{0} returned an error.\n Error code: {1}\n Error text:{2}",
                    operationName,
                    code,
                    text);
            }
            else
            {
                return string.Format(
                    "{0} returned an error.\n Error code: {1}\n Cause:{2}",
                    operationName,
                    code,
                    cause);
            }
        }
    }
}
