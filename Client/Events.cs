namespace Boerman.TcpLib.Client
{
    partial class TcpClient<TSend, TReceive>
    {
        protected virtual void OnReceiveEvent(TReceive data)
        {
            //Logger.Debug("OnReceiveEvent");

            OnReceiveEventHandler handler = ReceiveEvent;
            handler?.Invoke(data);
        }

        protected virtual void OnSendEvent()
        {
            //Logger.Debug("OnSendEvent");

            OnSendEventHandler handler = SendEvent;
            handler?.Invoke();
        }

        protected virtual void OnConnectEvent()
        {
            //Logger.Debug("OnConnectEvent");

            OnConnectEventHandler handler = ConnectEvent;
            handler?.Invoke();
        }

        protected virtual void OnDisconnectEvent()
        {
            //Logger.Debug("OnDisconnectEvent");

            OnDisconnectEventHandler handler = DisconnectEvent;
            handler?.Invoke();
        }
    }
}
