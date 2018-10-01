using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Boerman.Networking
{
    public class TcpServer : IDisposable
    {
        readonly ConcurrentDictionary<EndPoint, StateObject> _handlers = new ConcurrentDictionary<EndPoint, StateObject>();
        readonly ManualResetEvent _tcpServerActive = new ManualResetEvent(false);

        readonly ConnectionSettings _settings;

        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Boerman.Networking.TcpServer"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint to listen on for incoming connections.</param>
        /// <param name="certificate">The certificate to use in case you wish to use SSL/TLS.</param>
        public TcpServer(IPEndPoint endpoint, 
                         X509Certificate2 certificate = null)
        {
            _settings = new ConnectionSettings
            {
                EndPoint = endpoint,
                Encoding = Encoding.GetEncoding("utf-8"),
                UseSsl = certificate != null,
                Certificate = certificate
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Boerman.Networking.TcpServer"/> class.
        /// </summary>
        /// <param name="endpoint">The endpoint to listen on for incoming connections.</param>
        /// <param name="encoding">The text encoding you wish to use for this connection.</param>
        /// <param name="certificate">The certificate to use in case you wish to use SSL/TLS.</param>
        public TcpServer(IPEndPoint endpoint, 
                         Encoding encoding, 
                         X509Certificate2 certificate = null) {
            _settings = new ConnectionSettings()
            {
                EndPoint = endpoint,
                Encoding = encoding,
                UseSsl = certificate != null,
                Certificate = certificate
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
                Trace.TraceError(ex.ToString());
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

        public void Disconnect(EndPoint endpoint) {
            try
            {
                StateObject client;
                _handlers.TryGetValue(endpoint, out client);
                if (client == null) return;

                if (client.Socket.IsConnected())
                {
                    client.Socket.Shutdown(SocketShutdown.Both);
                    client.Socket.Disconnect(false);
                    // The disposal of the socket will happen in the endread method
                }

                client.Stream.Dispose();
                client.Socket.Dispose();

                _handlers.TryRemove(endpoint, out _);

                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(client.EndPoint));
            } catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
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
            try
            {
                _handlers.TryGetValue(endpoint, out StateObject clientState);

                if (clientState == null || !clientState.Socket.IsConnected()) return false;

                int dataLength = data.Length;

                return await Task.Factory.FromAsync(
                    (callback, state) => clientState.Stream.BeginWrite(data, 0, dataLength, new AsyncCallback(callback), state),
                    (arg) =>
                    {
                        clientState.Stream.EndWrite(arg);
                        return true;
                    }, null);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
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
            try
            {
                var listener = ((Socket)result.AsyncState);

                // End the accept and get ready to accept a new connection.
                Socket handler = listener.EndAccept(result);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                var state = new StateObject()
                {
                    Socket = handler,
                    EndPoint = handler.RemoteEndPoint
                };

                // ToDo: Add functionality to automatically check whether to use an SSL connection or just ol' plain tcp connection.
                if (_settings.UseSsl)
                {
                    state.Stream = new SslStream(new NetworkStream(state.Socket));

                    // ToDo: Handle the ssl handshake async
                    try
                    {
                        ((SslStream)state.Stream).AuthenticateAsServer(_settings.Certificate);
                    }
                    catch
                    {
                        // These errors can be mostly ignored. Connection should be closed.
                        Disconnect(state.EndPoint);
                    }
                }
                else
                {
                    state.Stream = new NetworkStream(state.Socket);
                }

                _handlers.TryAdd(state.EndPoint, state);

                Common.InvokeEvent(this, Connected, new ConnectedEventArgs(state));

                state.Stream.BeginRead(state.ReceiveBuffer, 0, state.ReceiveBufferSize, new AsyncCallback(ReadCallback), state);
            } catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        internal void ReadCallback(IAsyncResult result)
        {
            StateObject state = (StateObject)result.AsyncState;

            try
            {
                if (!state.Socket.IsConnected())
                {
                    Disconnect(state.EndPoint);
                    return;
                }

                int bytesRead = state.Stream.EndRead(result);

                if (bytesRead > 0)
                {
                    byte[] received = new byte[bytesRead];
                    Array.Copy(state.ReceiveBuffer, received, bytesRead);

                    Common.InvokeEvent(this, Received, new ReceivedEventArgs(state.EndPoint, received, _settings.Encoding));
                }

                // Honestly, I'd love to call this earlier but we're pretty prone to synchronization issues here...
                state.Stream.BeginRead(state.ReceiveBuffer, 0, state.ReceiveBufferSize, new AsyncCallback(ReadCallback), state);
            } catch (IOException ex) when (ex.InnerException is SocketException)
            {
                Debug.WriteLine(ex.ToString());
                Disconnect(state.EndPoint);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
