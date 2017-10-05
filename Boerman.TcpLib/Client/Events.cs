using System;
using System.Net;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient<TSend, TReceive>
    {
        public event EventHandler<OnReceiveEventArgs<TReceive>> OnReceive;
        public event EventHandler<OnConnectEventArgs> OnConnect;
        public event EventHandler<OnDisconnectEventArgs> OnDisconnect;

        public void InvokeOnReceiveEvent(TReceive data, EndPoint endpoint)
        {
            try
            {
                OnReceive?.Invoke(this, new OnReceiveEventArgs<TReceive>(data, endpoint));
            } catch { }
        }

        public void InvokeOnConnectEvent(EndPoint endpoint)
        {
            try
            {
                OnConnect?.Invoke(this, new OnConnectEventArgs(endpoint));
            } catch { }
        }

        public void InvokeOnDisconnectEvent(EndPoint endpoint)
        {
            try
            {
                OnDisconnect?.Invoke(this, new OnDisconnectEventArgs(endpoint));
            } catch { }
        }
    }
}
