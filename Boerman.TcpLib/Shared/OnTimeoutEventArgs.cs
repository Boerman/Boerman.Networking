using System;
using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class OnTimeoutEventArgs
    {
        public OnTimeoutEventArgs(EndPoint endpoint)
        {
            Endpoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public DateTime TimeStamp { get; }
        public EndPoint Endpoint { get; }
    }
}