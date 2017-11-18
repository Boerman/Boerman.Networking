using System;
using System.Linq;
using System.Text;
using Boerman.Core;
using Boerman.Core.Serialization;
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

                // We may as well back off if no connection is available.
                if (!state.Socket.IsConnected())
                {
                    state.Socket.Dispose(); // Bug: When the client is stopped using the Stop method this may be called faster then the stop method can do.

                    if (_clientSettings.ReconnectOnDisconnect)
                    {
                        Close();
                        Open();
                    }

                    return;
                }

                int bytesRead = state.Socket.EndReceive(result);

                var str = _clientSettings.Encoding.GetString(state.ReceiveBuffer, 0, bytesRead);
                InvokePartReceivedEvent(str, state.Endpoint);

                _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, ReceiveCallback, _state);
            }, ar);
        }
    }
}
