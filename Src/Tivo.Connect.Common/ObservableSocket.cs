using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;

namespace Tivo.Connect
{
    internal static class ObservableSocket
    {   
        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="addressFamily">One of the <see cref="AddressFamily"/> values.</param>
        /// <param name="socketType">One of the <see cref="SocketType"/> values.</param>
        /// <param name="protocolType">One of the <see cref="ProtocolType"/> values.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <returns>A singleton observable sequence containing the connected <see cref="Socket"/>.</returns>
        public static IObservable<Socket> Connect(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            EndPoint remoteEndPoint)
        {
            var socket = new Socket(addressFamily, socketType, protocolType);

            return Connect(socket, remoteEndPoint).Select(_ => socket);
        }

        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket that will create the connection.</param>
        /// <param name="address">The <see cref="IPAddress"/> of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <returns>A singleton observable sequence that indicates when the connection has been established.</returns>
        public static IObservable<Unit> Connect(Socket socket, IPAddress address, int port)
        {
            return Connect(socket, new IPEndPoint(address, port));
        }

        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket that will create the connection.</param>
        /// <param name="hostname">The name of the remote host.</param>
        /// <param name="port">The port number of the remote host.</param>
        /// <returns>A singleton observable sequence that indicates when the connection has been established.</returns>
        public static IObservable<Unit> Connect(Socket socket, string hostname, int port)
        {
            return Connect(socket, new DnsEndPoint(hostname, port));
        }

        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket that will create the connection.</param>
        /// <param name="remoteEndPoint">The <see cref="EndPoint"/> of the remote host.</param>
        /// <returns>A singleton observable sequence that indicates when the connection has been established.</returns>
        public static IObservable<Unit> Connect(Socket socket, EndPoint remoteEndPoint)
        {
            var args = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = remoteEndPoint
            };

            return Connect(socket, args)
                .Select(_ => Unit.Default)
                .Finally(args.Dispose);
        }

        /// <summary>
        /// Establishes a connection to a remote host.
        /// </summary>
        /// <param name="socket">The socket that will create the connection.</param>
        /// <param name="eventArgs">The <see cref="SocketAsyncEventArgs"/> object to use for this asynchronous socket operation.</param>
        /// <returns>A singleton observable sequence containing the result of the operation.</returns>
        /// <seealso href="http://msdn.microsoft.com/en-us/library/bb538102.aspx">Socket.ConnectAsync</seealso>
        public static IObservable<SocketAsyncEventArgs> Connect(Socket socket, SocketAsyncEventArgs eventArgs)
        {
            return eventArgs.InvokeAsync(socket.ConnectAsync);
        }
    }
}
