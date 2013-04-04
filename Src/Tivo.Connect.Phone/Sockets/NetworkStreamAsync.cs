using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public class NetworkStream : Stream
    {
        private readonly Socket _socket;
        private readonly bool _ownsSocket;
        private readonly FileAccess _access;

        private bool _isDisposed;

        public NetworkStream(Socket socket)
            : this(socket, FileAccess.ReadWrite, false) { }

        public NetworkStream(Socket socket, bool ownsSocket)
            : this(socket, FileAccess.ReadWrite, ownsSocket) { }

        public NetworkStream(Socket socket, FileAccess access)
            : this(socket, access, false) { }

        public NetworkStream(Socket socket, FileAccess access, bool ownsSocket)
        {
            _socket = socket;
            _access = access;
            _ownsSocket = ownsSocket;

            this.ReadTimeout = 10000;
            this.WriteTimeout = 5000;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && 
                _isDisposed)
            {
                if (_ownsSocket && _socket != null)
                {
                    _socket.Close();
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanRead
        {
            get { return (_access & FileAccess.Read) == FileAccess.Read; }
        }

        public override bool CanWrite
        {
            get { return (_access & FileAccess.Write) == FileAccess.Write; }
        }

        public override int WriteTimeout { get; set; }
        public override int ReadTimeout { get; set; }

        public override void Flush()
        {
            // does nothing
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }
        
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, offset, count);

            var tcs = new TaskCompletionSource<int>();

            args.Completed +=
                (sender, e) =>
                {
                    if (e.ConnectByNameError != null)
                    {
                        tcs.TrySetException(e.ConnectByNameError);
                    }
                    else
                    {
                        if (e.SocketError != SocketError.Success)
                        {
                            tcs.TrySetException(new SocketException((int)e.SocketError));
                        }
                        else
                        {
                            tcs.TrySetResult(e.BytesTransferred);
                        }
                    }
                };

            if (!_socket.ReceiveAsync(args))
            {
                return args.BytesTransferred;
            }

            if (!tcs.Task.Wait(this.ReadTimeout))
            {
                throw new IOException("Read operation timed out.");
            }

            return tcs.Task.Result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, offset, count);

            var tcs = new TaskCompletionSource<object>();

            args.Completed +=
                (sender, e) =>
                {
                    if (e.ConnectByNameError != null)
                    {
                        tcs.TrySetException(e.ConnectByNameError);
                    }
                    else
                    {
                        if (e.SocketError != SocketError.Success)
                        {
                            tcs.TrySetException(new SocketException((int)e.SocketError));
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                };

            if (!_socket.SendAsync(args))
            {
                return;
            }

            if (!tcs.Task.Wait(this.WriteTimeout))
            {
                throw new IOException("Write operation timed out.");
            }
        }
    }
}
