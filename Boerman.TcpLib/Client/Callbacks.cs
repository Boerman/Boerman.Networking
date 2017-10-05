using System;
using System.Linq;
using System.Text;
using Boerman.Core;
using Boerman.Core.Serialization;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient<TSend, TReceive>
        where TSend : class
        where TReceive : class
    {
        private void ConnectCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;
                state.WorkSocket.EndConnect(result);

                _isConnected.Set();
            }, ar);
        }

        private void SendCallback(IAsyncResult ar)
        {
            StateObject state = null;
            int bytesSend = 0;

            ExecuteFunction(delegate(IAsyncResult result)
            {
                state = (StateObject) result.AsyncState;
                bytesSend = state.WorkSocket.EndSend(result);
            }, ar);

            // If code down here is put in the `ExecuteFunction` wrapper and shit hits the fan
            // `_isSending` reset event would never be set thus never allowing the socket to close.

            if (state == null)
            {
                // Just make sure the program can continue running.
                _isSending.Set();
                return;
            }

            state.OutboundBuffer = state.OutboundBuffer.Skip(bytesSend).ToArray();

            if (state.OutboundBuffer.Length > 0)
                _state.WorkSocket.BeginSend(_state.OutboundBuffer, 0, _state.OutboundBuffer.Length, 0, SendCallback, _state);
            else
                _isSending.Set();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;

                // We may as well back off if no connection is available.
                if (!state.WorkSocket.IsConnected())
                {
                    state.WorkSocket.Dispose(); // Bug: When the client is stopped using the Stop method this may be called faster then the stop method can do.

                    if (_clientSettings.ReconnectOnDisconnect)
                    {
                        Close();
                        Open();
                    }

                    return;
                }

                int bytesRead = state.WorkSocket.EndReceive(result);
                
                state.InboundStringBuilder.Append(Encoding.GetEncoding(Constants.Encoding).GetString(state.ReceiveBuffer, 0, bytesRead));
                string content = state.InboundStringBuilder.ToString();

                while (content.IndexOf(_clientSettings.Splitter, StringComparison.Ordinal) > -1)
                {
                    var strParts = content.Split(new string[] { _clientSettings.Splitter },
                        StringSplitOptions.RemoveEmptyEntries);

                    var type = typeof(TReceive);
                    if (type == typeof(String))
                    {
                        InvokeOnReceiveEvent(strParts[0] + _clientSettings.Splitter as TReceive, _clientSettings.EndPoint);
                    }
                    else
                    {
                        // Convert it to the specific object.
                        var obj = ObjectDeserializer<TReceive>.Deserialize(Encoding.GetEncoding(Constants.Encoding).GetBytes(strParts[0]));
                        InvokeOnReceiveEvent(obj, _clientSettings.EndPoint);
                    }

                    state.ReceiveBuffer = new byte[state.ReceiveBufferSize];
                    state.InboundStringBuilder.Clear();

                    content = content.Remove(0, strParts[0].Length);
                    content = content.Remove(0, _clientSettings.Splitter.Length);

                    state.InboundStringBuilder.Append(content);
                }

                _state.WorkSocket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, ReceiveCallback, _state);
            }, ar);
        }
    }
}
