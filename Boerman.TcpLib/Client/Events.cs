using System;
using System.Net;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient<TSend, TReceive>
    {
        public event EventHandler<DataReceivedEventArgs<TReceive>> DataReceived;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        private void InvokeDataReceivedEvent(TReceive data, EndPoint endpoint)
        {
            try
            {
                DataReceived?.Invoke(this, new DataReceivedEventArgs<TReceive>(data, endpoint));
            } catch { }
        }

        private void InvokeConnectedEvent(EndPoint endpoint)
        {
            try
            {
                Connected?.Invoke(this, new ConnectedEventArgs(endpoint));
            } catch { }
        }

        private void InvokeDisconnectedEvent(EndPoint endpoint)
        {
            try
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(endpoint));
            } catch { }
        }
    }
}
