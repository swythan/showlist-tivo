﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using Org.BouncyCastle.Crypto.Tls;

namespace Tivo.Connect
{
    public class TivoConnection : TivoConnectionBase
    {
        TcpClient client;
        TlsProtocolHandler protocolHandler;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (this.protocolHandler != null)
                {
                    this.protocolHandler.Close();
                    this.protocolHandler = null;
                }

                if (this.client != null)
                {
                    this.client.Dispose();
                    Debug.WriteLine("Client closed.");

                    this.client = null;
                }
            }
        }

        protected override Stream ConnectNetworkStream(string serverAddress)
        {
            if (this.client != null)
            {
                throw new InvalidOperationException("Cannot open the same connection twice.");
            }


            // Create a TCP/IP connection to the TiVo.
            this.client = new TcpClient(serverAddress, 1413);

            Debug.WriteLine("Client connected.");

            // Create an SSL stream that will close the client's stream.
            this.protocolHandler = new TlsProtocolHandler(this.client.GetStream());

            try
            {
                this.protocolHandler.Connect( new TivoTlsClient() );
            }
            catch (IOException e)
            {
                Debug.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Debug.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Debug.WriteLine("Authentication failed - closing the connection.");
                client.Dispose();

                throw;
            }

            return protocolHandler.Stream;
        }

    }
}