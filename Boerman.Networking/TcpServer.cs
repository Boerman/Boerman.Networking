using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Boerman.Networking
{
    public class TcpServer
    {
        readonly ConcurrentDictionary<EndPoint, StateObject> _handlers = new ConcurrentDictionary<EndPoint, StateObject>();
        // ToDo: Replace the _tcpServerActive ManualResetEvent with a CancellationToken
        readonly ManualResetEvent _tcpServerActive = new ManualResetEvent(false);

        readonly ConnectionSettings _settings;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ConnectedEventArgs> Connected;

        public TcpServer(IPEndPoint endpoint)
        {
            _settings = new ConnectionSettings
            {
                EndPoint = endpoint,
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpServer(ConnectionSettings settings)
        {
            _settings = settings;
        }

        public void ExecuteFunction(Action<IAsyncResult> action, IAsyncResult param)
        {
            try
            {
                try
                {
                    action(param);
                }
                catch (SocketException ex)
                {
                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // Force disconnect
                            if (param.AsyncState is StateObject)
                            {
                                var state = (StateObject)param.AsyncState;
                                Disconnect(state.Endpoint);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());

                    if (param.AsyncState is StateObject) {
                        var state = (StateObject)param.AsyncState;
                        Disconnect(state.Endpoint);
                    }
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Start this instance.
        /// </summary>
        public void Start()
        {
            if (_tcpServerActive.WaitOne(0)) return;

            _tcpServerActive.Reset();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(_settings.EndPoint);
            } catch (SocketException ex) {
                if (ex.NativeErrorCode == 48) { // Address already in use
                    // ToDo: Add some usable logging information
                    throw;  // Rethrow
                }
            }

            listener.Listen(1000); // The number of allowed pending connections

            // Get ready to accept the first connection
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop()
        {
            // The TCP server is not running so no need to stop it.
            if (!_tcpServerActive.WaitOne(0)) return;

            // Stop the tcp server and ditch all the connections.
            foreach (var handler in _handlers)
            {
                handler.Value.Socket.Dispose();
                StateObject stateObject;
                _handlers.TryRemove(handler.Key, out stateObject);
            }
            _tcpServerActive.Set();
        }

        public void Disconnect(EndPoint endpoint, bool cleanUpOnly = false)
        {
            StateObject client;
            _handlers.TryGetValue(endpoint, out client);
            if (client == null) return;

            if (client.Socket.IsConnected())
            {
                client.Socket.Shutdown(SocketShutdown.Both);
                client.Socket.Disconnect(false);
            }

            client.Socket.Dispose();

            _handlers.TryRemove(endpoint, out _);

            if (!cleanUpOnly)
            {
                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(client.Endpoint));
            }
        }

        #region Send functions
        public void Send(EndPoint endpoint, string message)
        {
            // Send the message.
            Send(endpoint, _settings.Encoding.GetBytes(message));
        }

        public void Send(EndPoint endpoint, byte[] data)
        {
            // Check if this specific item is available.
            StateObject client;
            _handlers.TryGetValue(endpoint, out client);

            if (client == null || !client.Socket.IsConnected()) return;

            // Send the message.
            client.Socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), client);
        }

        /// <summary>
        /// Will send the message to all connected clients
        /// </summary>
        /// <param name="message">Message</param>
        public void Send(string message)
        {
            Send(_settings.Encoding.GetBytes(message));
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

                Send(handler.Endpoint, data);
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
                _handlers.TryAdd(state.Endpoint, state);

                Common.InvokeEvent(this, Connected, new ConnectedEventArgs(state.Endpoint));

                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                if (state != null) // Bug: When this happens something probably went wrong within the _handlers.TryGetValue call
                {
                    handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, new AsyncCallback(ReadCallback), state);
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
                Socket handler = state.Socket;

                if (!handler.IsConnected()) {
                    Disconnect(state.Endpoint);
                    return;
                }

                int bytesRead = handler.EndReceive(result);
                byte[] received = new byte[bytesRead];
                Array.Copy(state.ReceiveBuffer, received, bytesRead);

                Common.InvokeEvent(
                    this,
                    DataReceived,
                    new DataReceivedEventArgs(state.Endpoint, received));

                // Honestly, I'd love to call this earlier but we're pretty prone to synchronization issues here...
                handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0,
                                     new AsyncCallback(ReadCallback), state);
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
    }
}
