using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using Boerman.Core;
using Boerman.Core.Serialization;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Server
{
    partial class TcpServer<TSend, TReceive> 
        where TSend : class
        where TReceive : class
    {
        /// <summary>
        /// The callback used after a connection is accepted.
        /// </summary>
        /// <param name="ar"></param>
        internal void AcceptCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate(IAsyncResult result)
            {
                // The thread may continue to accept connections
                _allDone.Set();

                Socket handler = ((Socket)result.AsyncState).EndAccept(result);

                var rep = handler.RemoteEndPoint as IPEndPoint;
                
                var stateGuid = Guid.NewGuid();

                _handlers.TryAdd(stateGuid, new StateObject
                {
                    WorkSocket = handler,
                    Guid       = stateGuid,
                    IpAddress  = rep.Address,
                    Port       = rep.Port
                });

                StateObject state;
                _handlers.TryGetValue(stateGuid, out state);

                RaiseConnectEvent(state);

                // Strange situation when this happens, but it basically means that the connection is closed 
                // before the software had any change of accepting it (it's possible)
                // Maybe we should think about a better application architecture.
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

                Socket handler = state.WorkSocket;
                
                int bytesRead = handler.EndReceive(result);

                //if (bytesRead <= 0) return;

                state.InboundStringBuilder.Append(Encoding.GetEncoding(Constants.Encoding).GetString(state.ReceiveBuffer, 0, bytesRead));
                string content = state.InboundStringBuilder.ToString();

                // Keep looping until all messages in the buffer are processed
                if (state.ExpectedBytesCount < 1 || typeof(TReceive) != typeof(String))
                {
                    while (content.IndexOf(_serverSettings.Splitter, StringComparison.Ordinal) > -1)
                    {
                        var strParts = content.Split(new string[] {_serverSettings.Splitter},
                            StringSplitOptions.RemoveEmptyEntries);

                        var type = typeof(TReceive);
                        if (type == typeof(String))
                        {
                            RaiseReceiveEvent(state, strParts[0] + _serverSettings.Splitter as TReceive);
                        }
                        else
                        {
                            // Convert it to the specific object.
                            var obj =
                                ObjectDeserializer<TReceive>.Deserialize(
                                    Encoding.GetEncoding(Constants.Encoding).GetBytes(strParts[0]));

                            RaiseReceiveEvent(state, obj);
                        }

                        state.ReceiveBuffer = new byte[state.ReceiveBufferSize];
                        state.InboundStringBuilder.Clear();

                        content = content.Remove(0, strParts[0].Length);
                        content = content.Remove(0, _serverSettings.Splitter.Length);

                        state.InboundStringBuilder.Append(content);
                    }
                }
                else
                {
                    /*
                     * 27/02/2017
                     * Please note that this code is specificially written for HTTP/RTSP like protocols
                     */
                    if (content.Length >= state.ExpectedBytesCount)
                    {
                        RaiseReceiveEvent(state, content.Substring(0, state.ExpectedBytesCount) as TReceive);
                        content = content.Remove(0, state.ExpectedBytesCount);

                        // Reset the byte count
                        state.ExpectedBytesCount = 0;

                        state.InboundStringBuilder.Clear();
                        state.InboundStringBuilder.Append(content);
                    }
                }

                if (handler.IsConnected())
                {
                    // Wait until more data is received.
                    var receiveEvent = handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0,
                        ReadCallback, state);
                }
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
                client?.WorkSocket.EndSend(result);
                RaiseSendEvent(client);
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
                        handler.Value.WorkSocket.Shutdown(SocketShutdown.Both);
                        handler.Value.WorkSocket.Disconnect(false);
                        handler.Value.WorkSocket.Dispose();
                        
                        _handlers.TryRemove(handler.Key, out StateObject stateObject);

                        RaiseDisconnectEvent(stateObject);
                        RaiseTimeoutEvent(stateObject);
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
