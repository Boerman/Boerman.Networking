using System;
using System.Net;
using System.Net.Sockets;

namespace Boerman.TcpLib.Shared
{
    public class StateObject
    {
        public StateObject(Socket socket, int receiveBufferSize = 65535)
        {
            Socket = socket;
            Endpoint = socket.RemoteEndPoint;

            ReceiveBufferSize = receiveBufferSize;
            ReceiveBuffer = new byte[ReceiveBufferSize];

            LastReceived = DateTime.UtcNow;
            LastSend = DateTime.UtcNow;
        }

        public Socket Socket { get; }

        public int ReceiveBufferSize { get; }

        internal byte[] ReceiveBuffer;

        public DateTime LastReceived { get; internal set; }
        public DateTime LastSend { get; internal set; }

        // Do NOT retrieve this value from the Socket as it isn't available
        // anymore after the socket is disposed.
        public EndPoint Endpoint { get; set; }
    }
}
