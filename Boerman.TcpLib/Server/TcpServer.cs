using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        /// <summary>
        /// The timer being used to register timeouts on the sockets.
        /// </summary>
        private readonly Timer _timeoutTimer = new Timer(1000);
        
        public TcpServer(IPEndPoint endpoint)
        {
            _serverSettings = new ServerSettings
            {
                IpEndPoint = endpoint,
                Splitter = "\r\n",
                ClientTimeout = 300000,
                ReuseAddress = false,
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpServer(ServerSettings serverSettings)
        {
            _serverSettings = serverSettings;
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

            InvokeDisconnectedEvent(client.Endpoint);
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
    }
}
