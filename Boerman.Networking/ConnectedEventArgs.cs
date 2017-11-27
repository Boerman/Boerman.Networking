using System;
using System.Net;

namespace Boerman.Networking
{
    public class ConnectedEventArgs
    {
        internal ConnectedEventArgs(EndPoint endpoint)
        {
            EndPoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public EndPoint EndPoint { get; }
        public DateTime TimeStamp { get; }
    }
}
