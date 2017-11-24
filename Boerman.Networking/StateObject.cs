using System.Net;
using System.Net.Sockets;

namespace Boerman.Networking
{
    class StateObject
    {
        internal StateObject(Socket socket, int receiveBufferSize = 65535)
        {
            Socket = socket;
            Endpoint = socket.RemoteEndPoint;

            ReceiveBufferSize = receiveBufferSize;
            ReceiveBuffer = new byte[ReceiveBufferSize];
        }

        public Socket Socket { get; }

        public int ReceiveBufferSize { get; }

        internal byte[] ReceiveBuffer;

        // Do NOT retrieve this value from the Socket as it isn't available
        // anymore after the socket is disposed.
        public EndPoint Endpoint { get; set; }
    }
}
