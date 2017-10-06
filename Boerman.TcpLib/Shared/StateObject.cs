using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Boerman.TcpLib.Shared
{
    public class StateObject
    {
        public StateObject(Socket socket, int receiveBufferSize = 65536)
        {
            Guid = Guid.NewGuid();

            Socket = socket;

            this.ReceiveBufferSize = receiveBufferSize;
            ReceiveBuffer = new byte[ReceiveBufferSize];
            
        }

        public Guid Guid { get; }

        public Socket Socket { get; }

        public int ReceiveBufferSize { get; }

        internal byte[] ReceiveBuffer;
        internal byte[] SendBuffer;

        internal ConcurrentQueue<byte[]> OutboundMessages = new ConcurrentQueue<byte[]>();

        internal StringBuilder InboundStringBuilder = new StringBuilder();
        internal StringBuilder OutboundStringBuilder = new StringBuilder();
        
        public DateTime LastConnection { get; internal set; }

        // Initialize both values down here because they're used to define the timeout values.
        public DateTime LastReceived { get; internal set; }
        public DateTime LastSend { get; internal set; }

        public EndPoint Endpoint => Socket.RemoteEndPoint;

        public int ExpectedBytesCount = 0;
    }
}
