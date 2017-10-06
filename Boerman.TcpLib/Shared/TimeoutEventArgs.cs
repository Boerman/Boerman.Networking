using System;
using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class TimeoutEventArgs
    {
        public TimeoutEventArgs(EndPoint endpoint)
        {
            Endpoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public DateTime TimeStamp { get; }
        public EndPoint Endpoint { get; }
    }
}