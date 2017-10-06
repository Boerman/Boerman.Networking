using System;
using System.Net;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Server
{
    partial class TcpServer<TSend, TReceive>
    {
        public event EventHandler<DataReceivedEventArgs<TReceive>> DataReceived;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<TimeoutEventArgs> Timeout;
        
        private void InvokeDataReceivedEvent(TReceive data, EndPoint endpoint)
        {
            try
            {
                DataReceived?.Invoke(this, new DataReceivedEventArgs<TReceive>(data, endpoint));
            } catch { }
        }

        private void InvokeDisconnectedEvent(EndPoint endpoint)
        {
            try
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(endpoint));
            } catch { }
        }

        private void InvokeConnectedEvent(EndPoint endpoint)
        {
            try
            {
                Connected?.Invoke(this, new ConnectedEventArgs(endpoint));
            } catch { }
        }

        private void InvokeTimeoutEvent(EndPoint endpoint)
        {
            try
            {
                Timeout?.Invoke(this, new TimeoutEventArgs(endpoint));
            } catch { }
        }
    }
}
