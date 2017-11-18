using System;
using System.Net;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient
    {
        public event EventHandler<DataReceivedEventArgs<string>> DataReceived;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<DataReceivedEventArgs<string>> PartReceived;

        private void InvokeDataReceivedEvent(string data, EndPoint endpoint)
        {
            try
            {
                DataReceived?.Invoke(this, new DataReceivedEventArgs<string>(data, endpoint));
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

        private void InvokePartReceivedEvent(string data, EndPoint endpoint)
        {
            try
            {
                PartReceived?.Invoke(this, new DataReceivedEventArgs<string>(data, endpoint));
            }
            catch { }
        }
    }
}
