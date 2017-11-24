using System;
using System.Net;

namespace Boerman.Networking
{
    public class ConnectedEventArgs
    {
        public ConnectedEventArgs(EndPoint endpoint)
        {
            Endpoint = endpoint;
            TimeStamp = DateTime.UtcNow;
        }

        public EndPoint Endpoint { get; }
        public DateTime TimeStamp { get; set; }
    }
}