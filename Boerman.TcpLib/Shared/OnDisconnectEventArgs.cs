using System;
using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class OnDisconnectEventArgs
    {
        public OnDisconnectEventArgs(EndPoint endpoint)
        {
            Endpoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public EndPoint Endpoint { get; }
        public DateTime TimeStamp { get; }
    }
}