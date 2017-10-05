using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Boerman.Core;
using Boerman.Core.Serialization;
using Boerman.TcpLib.Shared;
using Timer = System.Timers.Timer;

namespace Boerman.TcpLib.Server
{
    public partial class TcpServer<TSend, TReceive>
        where TSend : class
        where TReceive : class
    {
        private readonly ConcurrentDictionary<Guid, StateObject> _handlers = new ConcurrentDictionary<Guid, StateObject>();
        private readonly ManualResetEvent _allDone                         = new ManualResetEvent(false);
        private readonly ManualResetEvent _tcpServerActive                 = new ManualResetEvent(false);
        private readonly ServerSettings _serverSettings;

        /// <summary>
        /// The timer being used to register timeouts on the sockets.
        /// </summary>
        private readonly Timer _timeoutTimer = new Timer(1000);

        public TcpServer()
        {
            _serverSettings = new ServerSettings
            {
                IpEndPoint          = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 36700),
                Splitter            = "\r\n",
                Listening           = false,
                ClientTimeout       = 1020000, // 17 minutes in milliseconds
                ReuseAddress        = false,
                DontLinger          = false
            };
        }

        public TcpServer(ServerSettings serverSettings)
        {
            _serverSettings = serverSettings;

            // Directly start the server!
            if (_serverSettings.Listening) Run();
        }

        //// EVENTS //
        //public event OnReceiveEventHandler              ReceiveEvent;
        //public event OnSendEventHandler                 SendEvent;
        //public event OnConnectEventHandler              ConnectEvent;
        //public event OnDisconnectEventHandler           DisconnectEvent;
        //public event OnTimeoutEventHandler              TimeoutEvent;
        //public event OnExceptionEventHandler            ExceptionEvent;

        //public delegate void OnReceiveEventHandler      (StateObject state, TReceive content);
        //public delegate void OnSendEventHandler         (StateObject state);
        //public delegate void OnConnectEventHandler      (StateObject state);
        //public delegate void OnDisconnectEventHandler   (StateObject state);
        //public delegate void OnTimeoutEventHandler      (StateObject state);
        //public delegate void OnExceptionEventHandler    (StateObject state);

        // FUNCTIONS //
        public void Run()
        {
            if (_serverSettings.Listening) return;

            // Enable the timer. Make sure it's only registered once.
            _timeoutTimer.Elapsed -= TimeoutTimerOnElapsed;
            _timeoutTimer.Elapsed += TimeoutTimerOnElapsed;
            _timeoutTimer.Start();

            _serverSettings.Listening = true;
            _tcpServerActive.Reset();

            // When we stop the connections are gracefully dropped, at least
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Some configuration options
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, _serverSettings.DontLinger);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, _serverSettings.ReuseAddress);

                listener.Bind(_serverSettings.IpEndPoint);
                listener.Listen(1000); // The number of allowed pending connections

                while (_serverSettings.Listening)
                {
                    _allDone.Reset();
                    listener.BeginAccept(AcceptCallback, listener);
                    _allDone.WaitOne();
                }
            }
            
            _handlers.Clear();  // The worksockets in here are not available anymore.

            _tcpServerActive.Set();
        }

        public void Stop()
        {
            if (!_serverSettings.Listening) return;
            _serverSettings.Listening = false;

            // Stop the timer as all connections are about to be ditched anyway.
            _timeoutTimer.Stop();

            // Officially it can take up to 4 minutes for connections to be disposed. (After 4 minutes it's dropped by the OS)
            _tcpServerActive.WaitOne(new TimeSpan(0, 10, 0));


            // Stop the tcp server and ditch all the connections.
            foreach (var handler in _handlers)
            {
                handler.Value.WorkSocket.Dispose();
                StateObject stateObject;
                _handlers.TryRemove(handler.Key, out stateObject);
            }
        }

        public void Restart()
        {
            Stop();
            Run();
        }

        public void Disconnect(Guid target)
        {
            StateObject client;
            _handlers.TryGetValue(target, out client);
            if (client == null) return;

            client.WorkSocket.Shutdown(SocketShutdown.Both);
            client.WorkSocket.Disconnect(false);
            client.WorkSocket.Dispose();

            StateObject stateObject;

            _handlers.TryRemove(target, out stateObject);
        }

        #region Send functions
        public void Send(Guid target, string message)
        {
            // Send the message.
            Send(target, Encoding.GetEncoding(Constants.Encoding).GetBytes(message));
        }

        public void Send(Guid target, TSend obj)
        {
            var splitter = Encoding.GetEncoding(Constants.Encoding).GetBytes(_serverSettings.Splitter);
            var array = ObjectSerializer.Serialize(obj).Concat(splitter).ToArray();

            Send(target, array);
        }

        private void Send(Guid id, byte[] data)
        {
            // Check if this specific item is available.
            StateObject client;
            _handlers.TryGetValue(id, out client);

            if (client == null || !client.WorkSocket.IsConnected()) return;

            // Send the message.
            client.WorkSocket.BeginSend(data, 0, data.Length, 0, SendCallback, client);        
        }

        public void Send(string message)
        {
            SendToAll(Encoding.GetEncoding(Constants.Encoding).GetBytes(message));
        }

        public void Send(TSend obj)
        {
            var splitter = Encoding.GetEncoding(Constants.Encoding).GetBytes(_serverSettings.Splitter);
            var array = ObjectSerializer.Serialize(obj).Concat(splitter).ToArray();

            SendToAll(array);
        }

        private void SendToAll(byte[] data)
        {
            foreach (var handler in _handlers.Values)
            {
                if (handler == null) continue;

                Send(handler.Guid, data);
            }
        }
        #endregion

        public int ConnectionCount()
        {
            return _handlers.Count();
        }
    }

    public class ServerSettings
    {
        public IPEndPoint   IpEndPoint      { get; set; }
        public string       Splitter        { get; set; }
        public bool         Listening       { get; set; }
        public int          ClientTimeout   { get; set; }
        public bool         DontLinger      { get; set; }
        public bool         ReuseAddress    { get; set; }
    }
}
