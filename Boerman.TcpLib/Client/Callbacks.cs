using System;
using System.Linq;
using System.Net.Sockets;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient
    {
        private void ConnectCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;
                state.Socket.EndConnect(result);

                _isConnected.Set();
                _isSending.Set();
            }, ar);
        }

        private void SendCallback(IAsyncResult ar)
        {
            StateObject state = null;
            int bytesSend = 0;

            ExecuteFunction(delegate(IAsyncResult result)
            {
                state = (StateObject) result.AsyncState;
                bytesSend = state.Socket.EndSend(result);
            }, ar);

            // If code down here is put in the `ExecuteFunction` wrapper and shit hits the fan
            // `_isSending` reset event would never be set thus never allowing the socket to close.

            if (state == null)
            {
                // Just make sure the program can continue running.
                _isSending.Set();
                return;
            }

            state.SendBuffer = state.SendBuffer.Skip(bytesSend).ToArray();

            //if (state.SendBuffer.Length > 0)
            //    _state.Socket.BeginSend(_state.SendBuffer, 0, _state.SendBuffer.Length, 0, SendCallback, _state);
            //else
            _isSending.Set();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;
                state.LastConnection = DateTime.UtcNow;

                Socket handler = state.Socket;

                if (handler.IsConnected()) {
                    handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, ReceiveCallback, state);
                }
                else {
                    Close();

                    // Don't ask what I need this for
                    if (_clientSettings.ReconnectOnDisconnect) {
                        Close();
                        Open();
                    }

                    return;
                }

                int bytesRead = handler.EndReceive(result);

                var str = _clientSettings.Encoding.GetString(state.ReceiveBuffer, 0, bytesRead);
                InvokePartReceivedEvent(str, state.Endpoint);
            }, ar);
        }
    }
}
