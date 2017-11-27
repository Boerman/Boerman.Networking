using System;
using System.Net;

namespace Boerman.Networking
{
    public class DisconnectedEventArgs
    {
        internal DisconnectedEventArgs(EndPoint endpoint)
        {
            EndPoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public EndPoint EndPoint { get; }
        public DateTime TimeStamp { get; }
    }
}
