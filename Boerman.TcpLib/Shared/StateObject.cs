using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Boerman.TcpLib.Shared
{
    public class StateObject
    {
        public Socket WorkSocket = null;
        public Guid Guid;

        internal int ReceiveBufferSize = 65536;
        internal byte[] ReceiveBuffer;
        
        internal byte[] OutboundBuffer;

        internal ConcurrentQueue<byte[]> OutboundMessages = new ConcurrentQueue<byte[]>();

        internal StringBuilder InboundStringBuilder = new StringBuilder();
        internal StringBuilder OutboundStringBuilder = new StringBuilder();
        
        public DateTime LastConnection = DateTime.UtcNow;

        // Initialize both values down here because they're used to define the timeout values.
        public DateTime LastReceived = DateTime.UtcNow;
        public DateTime LastSend = DateTime.UtcNow;
        
        //public IPAddress IpAddress;
        //public int Port;

        public EndPoint Endpoint;

        public int ExpectedBytesCount = 0;

        public StateObject()
        {
            ReceiveBuffer = new byte[ReceiveBufferSize];
        }
    }
}
