using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using Boerman.TcpLib.Shared;
using Timer = System.Timers.Timer;

namespace Boerman.TcpLib.Server
{
    public partial class TcpServer
    {
        private readonly ConcurrentDictionary<Guid, StateObject> _handlers = new ConcurrentDictionary<Guid, StateObject>();
        // ToDo: Replace the _tcpServerActive ManualResetEvent with a CancellationToken
        private readonly ManualResetEvent _tcpServerActive                 = new ManualResetEvent(false);
        private readonly ServerSettings _serverSettings;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<TimeoutEventArgs> Timeout;

        /// <summary>
        /// The timer being used to register timeouts on the sockets.
        /// </summary>
        private readonly Timer _timeoutTimer = new Timer(1000);
        
        public TcpServer(IPEndPoint endpoint)
        {
            _serverSettings = new ServerSettings
            {
                IpEndPoint = endpoint,
                ClientTimeout = 30000,
                ReuseAddress = false,
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpServer(ServerSettings serverSettings)
        {
            _serverSettings = serverSettings;
        }

        /// <summary>
        /// This function executes the given function and handles exceptions, if any.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="param">The param with which the action has to be executed.</param>
        /// <returns></returns>
        public void ExecuteFunction(Action<IAsyncResult> action, IAsyncResult param)
        {
            try
            {
                try
                {
                    action(param);
                }
                catch (ObjectDisposedException)
                {
                    if (param.AsyncState is StateObject)
                    {
                        // TcpClient is already closed. All we gotta do is remove all references. (Aaand the garbage man will clean the shit behind our backs.)
                        StateObject state = (StateObject)param.AsyncState;

                        Disconnect(state.Guid);
                    }
                }
                catch (InvalidOperationException)
                {
                    if (param.AsyncState is StateObject)
                    {
                        // Tcp client isn't connected (We'd better clean up the resources.)
                        StateObject state = (StateObject)param.AsyncState;

                        Disconnect(state.Guid);
                    }
                }
                catch (SocketException ex)
                {
                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // Force disconnect
                            if (param.AsyncState is StateObject)
                            {
                                var state = (StateObject)param.AsyncState;

                                Disconnect(state.Guid);
                            }
                            break;
                    }
                }
                catch (NullReferenceException)
                {
                    // If this happens we're really far away! Restart the socket listener!
                    Restart();
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);        // MAYDAY MAYDAY MAYDAY
            }
        }

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {
            if (_tcpServerActive.WaitOne(0)) return;

            // Enable the timer. Make sure it's only registered once.
            _timeoutTimer.Elapsed -= TimeoutTimerOnElapsed;
            _timeoutTimer.Elapsed += TimeoutTimerOnElapsed;
            _timeoutTimer.Start();
            
            _tcpServerActive.Reset();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _serverSettings.ReuseAddress);

            try
            {
                listener.Bind(_serverSettings.IpEndPoint);
            } catch (SocketException ex) {
                if (ex.NativeErrorCode == 48) { // Address already in use
                    // ToDo: Add some usable logging information
                    throw;  // Rethrow
                }
            }

            listener.Listen(1000); // The number of allowed pending connections

            // Get ready to accept the first connection
            listener.BeginAccept(AcceptCallback, listener);
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop()
        {
            // The TCP server is not running so no need to stop it.
            if (_tcpServerActive.WaitOne(0)) return;

            // Stop the timer as all connections are about to be ditched anyway.
            _timeoutTimer.Stop();

            // Stop the tcp server and ditch all the connections.
            foreach (var handler in _handlers)
            {
                handler.Value.Socket.Dispose();
                StateObject stateObject;
                _handlers.TryRemove(handler.Key, out stateObject);
            }
            _tcpServerActive.Set();
        }

        /// <summary>
        /// Restart this instance.
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Disconnect the specified client.
        /// </summary>
        /// <param name="clientId">Client</param>
        public void Disconnect(Guid clientId)
        {
            StateObject client;
            _handlers.TryGetValue(clientId, out client);
            if (client == null) return;

            if (client.Socket.IsConnected())
            {
                client.Socket.Shutdown(SocketShutdown.Both);
                client.Socket.Disconnect(false);
            }

            client.Socket.Dispose();

            _handlers.TryRemove(clientId, out _);

            Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(client.Endpoint));
        }

        #region Send functions
        /// <summary>
        /// Send the message to a specified client
        /// </summary>
        /// <param name="clientId">Client identifier</param>
        /// <param name="message">Message</param>
        public void Send(Guid clientId, string message)
        {
            // Send the message.
            Send(clientId, _serverSettings.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Send the data to a specified client
        /// </summary>
        /// <param name="clientId">Client identifier</param>
        /// <param name="data">Data</param>
        public void Send(Guid clientId, byte[] data)
        {
            // Check if this specific item is available.
            StateObject client;
            _handlers.TryGetValue(clientId, out client);

            if (client == null || !client.Socket.IsConnected()) return;

            // Send the message.
            client.Socket.BeginSend(data, 0, data.Length, 0, SendCallback, client);        
        }

        /// <summary>
        /// Will send the message to all connected clients
        /// </summary>
        /// <param name="message">Message</param>
        public void Send(string message)
        {
            Send(_serverSettings.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Will send the data to all connected clients
        /// </summary>
        /// <param name="data">Data</param>
        public void Send(byte[] data)
        {
            foreach (var handler in _handlers.Values)
            {
                if (handler == null) continue;

                Send(handler.Guid, data);
            }
        }
        #endregion

        /// <summary>
        /// Returns the current number of connected clients
        /// </summary>
        /// <returns>Number of connected clients</returns>
        public int ConnectionCount()
        {
            return _handlers.Count();
        }

        /// <summary>
        /// The callback used after a connection is accepted.
        /// </summary>
        /// <param name="ar"></param>
        internal void AcceptCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate (IAsyncResult result)
            {
                var listener = ((Socket)result.AsyncState);

                // End the accept and get ready to accept a new connection.
                Socket handler = listener.EndAccept(result);

                var state = new StateObject(handler);
                _handlers.TryAdd(state.Guid, state);

                Common.InvokeEvent(this, Connected, new ConnectedEventArgs(state.Endpoint));

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
            ExecuteFunction(delegate (IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;
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
                    Disconnect(state.Guid);
                    return;
                }

                int bytesRead = handler.EndReceive(result);

                var str = _serverSettings.Encoding.GetString(state.ReceiveBuffer, 0, bytesRead);
                Common.InvokeEvent(this, DataReceived, new DataReceivedEventArgs(str, state.Endpoint));
            }, ar);
        }

        /// <summary>
        ///  The callback used after data is sent over the socket.
        /// </summary>
        /// <param name="ar"></param>
        internal void SendCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate (IAsyncResult result)
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
                    Disconnect(handler.Key);
                    Common.InvokeEvent(this, Timeout, new TimeoutEventArgs(handler.Value.Endpoint));
                }
            }
        }
    }
}
