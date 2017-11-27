using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Boerman.Networking
{
    public class TcpServer
    {
        readonly ConcurrentDictionary<EndPoint, StateObject> _handlers = new ConcurrentDictionary<EndPoint, StateObject>();
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

        public TcpServer(IPEndPoint endpoint, Encoding encoding) {
            _settings = new ConnectionSettings()
            {
                EndPoint = endpoint,
                Encoding = encoding
            };
        }

        public bool Start()
        {
            if (_tcpServerActive.WaitOne(0)) return true;

            _tcpServerActive.Reset();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try {
                listener.Bind(_settings.EndPoint);
                listener.Listen(1000);
            } catch (Exception ex) {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                return false;
            }

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
            return true;
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop()
        {
            // The TCP server is not running so no need to stop it.
            if (!_tcpServerActive.WaitOne(0)) return;

            // Stop the tcp server and ditch all the connections.
            foreach (var handler in _handlers) Disconnect(handler.Key);

            _tcpServerActive.Set();
        }

        private void Disconnect(EndPoint endpoint, bool cleanUpOnly) {
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
                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(client.EndPoint));
            }
        }

        public void Disconnect(EndPoint endpoint)
        {
            Disconnect(endpoint, false);
        }

        /// <summary>
        /// Send the message to the specified endpoint
        /// </summary>
        /// <returns>A boolean value indicating whther the message was sent correctly</returns>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="message">Message.</param>
        public async Task<bool> Send(EndPoint endpoint, string message)
        {
            // Send the message.
            return await Send(endpoint, _settings.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Send the data to the specified endpoint
        /// </summary>
        /// <returns>A boolean value indicating whther the data was sent correctly</returns>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="data">Data.</param>
        public async Task<bool> Send(EndPoint endpoint, byte[] data)
        {
            _handlers.TryGetValue(endpoint, out StateObject clientState);

            if (clientState == null || !clientState.Socket.IsConnected()) return false;

            int dataLength = data.Length;

            int bytesSent = await Task.Factory.FromAsync(
                (callback, state) => clientState.Socket.BeginSend(data, 0, dataLength, 0, new AsyncCallback(callback), state),
                (arg) =>
                {
                    try
                    {
                        return clientState.Socket.EndSend(arg);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                        return 0;
                    }
                }, null);

            if (bytesSent == dataLength) return true;

            return false;
        }

        /// <summary>
        /// Send the message to all clients
        /// </summary>
        /// <param name="message">Message</param>
        public async Task Send(string message)
        {
            await Send(_settings.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Send the data to all clients
        /// </summary>
        /// <param name="data">Data.</param>
        public async Task Send(byte[] data)
        {
            foreach (var handler in _handlers.Values)
            {
                if (handler == null) continue;
                await Send(handler.EndPoint, data);
            }
        }

        /// <summary>
        /// Returns the current number of connected clients
        /// </summary>
        /// <returns>Number of connected clients</returns>
        public int ConnectionCount()
        {
            return _handlers.Count();
        }

        internal void AcceptCallback(IAsyncResult result)
        {
            var listener = ((Socket)result.AsyncState);

            // End the accept and get ready to accept a new connection.
            Socket handler = listener.EndAccept(result);
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

            var state = new StateObject() {
                Socket = handler,
                EndPoint = handler.RemoteEndPoint
            };

            _handlers.TryAdd(state.EndPoint, state);

            Common.InvokeEvent(this, Connected, new ConnectedEventArgs(state.EndPoint));

            try {
                handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, new AsyncCallback(ReadCallback), state);
            } catch (Exception ex) {
                System.Diagnostics.Trace.TraceError(ex.ToString());
            }
        }

        internal void ReadCallback(IAsyncResult result)
        {
            StateObject state = (StateObject)result.AsyncState;
            Socket handler = state.Socket;

            if (!handler.IsConnected())
            {
                Disconnect(state.EndPoint);
                return;
            }

            int bytesRead = handler.EndReceive(result);

            byte[] received = new byte[bytesRead];
            Array.Copy(state.ReceiveBuffer, received, bytesRead);

            Common.InvokeEvent(this, DataReceived, new DataReceivedEventArgs(state.EndPoint, received, _settings.Encoding));

            // Honestly, I'd love to call this earlier but we're pretty prone to synchronization issues here...
            handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, new AsyncCallback(ReadCallback), state);
        }
    }
}
