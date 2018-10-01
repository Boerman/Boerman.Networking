using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Boerman.Networking
{
    class StateObject
    {
        internal StateObject(int receiveBufferSize = 65535)
        {
            ReceiveBufferSize = receiveBufferSize;
            ReceiveBuffer = new byte[ReceiveBufferSize];
        }

        internal Socket Socket { get; set; }
        internal Stream Stream { get; set; }

        internal int ReceiveBufferSize { get; }

        internal byte[] ReceiveBuffer;

        // Do NOT retrieve this value from the Socket as it isn't available
        // anymore after the socket is disposed.
        internal EndPoint EndPoint { get; set; }

        internal Encoding Encoding { get; set; }

        internal bool UseSsl { get; set; }
    }
}
