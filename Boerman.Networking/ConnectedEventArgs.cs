using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Boerman.Networking
{
    public class ConnectedEventArgs
    {
        private StateObject _state;

        internal ConnectedEventArgs(StateObject state)
        {
            _state = state;

            EndPoint = state.EndPoint;
            TimeStamp = DateTime.UtcNow;
        }

        public EndPoint EndPoint { get; }
        public DateTime TimeStamp { get; }
        public NetworkStream ReadOnlyNetworkStream {
            get => new NetworkStream(_state.Socket, FileAccess.Read, false);
        }
    }
}
