using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Boerman.Networking
{
    internal class SocketStream
    {
        private Socket _socket;
        private Stream _stream;

        public SocketStream(Socket socket, bool useSsl = false) {
            _socket = socket;

            if (useSsl) {
                _stream = new SslStream(new NetworkStream(_socket));
            } else {
                _stream = new NetworkStream(_socket);
            }
        }


	}
}
