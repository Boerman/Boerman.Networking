using System;
using System.Net.Sockets;
using System.Timers;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Server
{
    partial class TcpServer
    {
        /// <summary>
        /// The callback used after a connection is accepted.
        /// </summary>
        /// <param name="ar"></param>
        internal void AcceptCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                var listener = ((Socket)result.AsyncState);

                // End the accept and get ready to accept a new connection.
                Socket handler = listener.EndAccept(result);

                var state = new StateObject(handler);
                _handlers.TryAdd(state.Guid, state);
                InvokeConnectedEvent(state.Endpoint);

                listener.BeginAccept(AcceptCallback, listener);

                if (state != null) // Bug: When this happens something probably went wrong within the _handlers.TryGetValue call
                {
                    handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, ReadCallback, state);
                }
            }, ar);
        }

        /// <summary>
        ///  The callback used after a read event is accepted.
        /// </summary>
        /// <param name="ar"></param>
        internal void ReadCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;

                // Update the lastConnectionmoment.
                state.LastConnection = DateTime.UtcNow;

                Socket handler = state.Socket;

                if (handler.IsConnected())
                {
                    // Immediately go get some more data
                    handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0,
                        ReadCallback, state);
                }
                else
                {
                    // We're disconnected
                    InvokeDisconnectedEvent(state.Endpoint);
                    return;
                }

                int bytesRead = handler.EndReceive(result);

                var str = _serverSettings.Encoding.GetString(state.ReceiveBuffer, 0, bytesRead);
                InvokePartReceivedEvent(str, state.Endpoint);
            }, ar);
        }

        /// <summary>
        ///  The callback used after data is sent over the socket.
        /// </summary>
        /// <param name="ar"></param>
        internal void SendCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                var client = result.AsyncState as StateObject;
                client?.Socket.EndSend(result);
            }, ar);
        }

        private void TimeoutTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            foreach (var handler in _handlers)
            {
                if ((DateTime.UtcNow - handler.Value.LastConnection).TotalMilliseconds > _serverSettings.ClientTimeout)
                {
                    try
                    {
                        handler.Value.Socket.Shutdown(SocketShutdown.Both);
                        handler.Value.Socket.Disconnect(false);
                        handler.Value.Socket.Dispose();
                        
                        _handlers.TryRemove(handler.Key, out StateObject stateObject);

                        InvokeDisconnectedEvent(stateObject.Endpoint);
                        InvokeTimeoutEvent(stateObject.Endpoint);
                    }
                    catch (Exception)
                    {
                        // To catch objectdisposedexceptions
                    }
                }
            }
        }
    }
}
