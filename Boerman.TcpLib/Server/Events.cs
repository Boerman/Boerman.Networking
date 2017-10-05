using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Server
{
    partial class TcpServer<TSend, TReceive>
    {
        protected virtual void RaiseReceiveEvent(StateObject state, TReceive content)
        {
            //Logger.Debug("OnReceiveEvent");
            //Logger.Trace(state);
            //Logger.Trace(content);

            OnReceiveEventHandler handler = ReceiveEvent;
            handler?.Invoke(state, content);
        }

        protected virtual void RaiseSendEvent(StateObject state)
        {
            //Logger.Debug("OnSendEvent");
            //Logger.Trace(state);

            OnSendEventHandler handler = SendEvent;
            handler?.Invoke(state);
        }

        protected virtual void RaiseConnectEvent(StateObject state)
        {
            //Logger.Debug("OnConnectEvent");
            //Logger.Trace(state);

            OnConnectEventHandler handler = ConnectEvent;
            handler?.Invoke(state);
        }

        protected virtual void RaiseDisconnectEvent(StateObject state)
        {
            //Logger.Debug("OnDisconnectEvent");
            //Logger.Trace(state);

            OnDisconnectEventHandler handler = DisconnectEvent;
            handler?.Invoke(state);
        }
        
        protected virtual void RaiseTimeoutEvent(StateObject state)
        {
            //Logger.Debug("OnTimeoutEvent");
            //Logger.Trace(state);

            OnTimeoutEventHandler handler = TimeoutEvent;
            handler?.Invoke(state);
        }

        protected virtual void RaiseExceptionEvent(StateObject state)
        {
            //Logger.Debug("OnExceptionEvent");
            //Logger.Trace(state);

            OnExceptionEventHandler handler = ExceptionEvent;
            handler?.Invoke(state);
        }
    }
}
