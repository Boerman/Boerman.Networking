using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Boerman.Networking
{
    public class TcpClient
    {
        StateObject _state;

        public event EventHandler<ReceivedEventArgs> Received;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public TcpClient()
        {
            _state = new StateObject
            {
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpClient(Encoding encoding) {
            _state = new StateObject
            {
                Encoding = encoding
            };
        }

        /// <summary>
        /// Open the connection to the remote endpoint.
        /// </summary>
        /// <returns>Boolean indicating whether the connection is open or not</returns>
        public async Task<bool> Open(EndPoint endpoint, 
                                     bool useSsl = false,
                                     bool allowCertificateChainErrors = false)
        {
            /*
             * To open a connection with a server is not exactly a synchronous 
             * operation with a predefined execution time. Because of this, and 
             * because it is almost always required to know whether the client 
             * is connected or not the Open method is async. The implementation
             * of this method makes sure a boolean value is returned indicating
             * whether the connection has succesfully opened or not.
             * 
             * In the future it may be possible we remove the boolean return
             * value in order to provide a more detailed return value indicating
             * what might have gone wrong in case of an unsuccesfull connection.
             * 
             * -----------------------------------------------------------------
             * 
             * As the asynchronous programming pattern used with the `Socket` 
             * class is not like the language standard we use the
             * Task.Factory.FromAsync method to couple the Begin... and End...
             * methods.
             * 
             * After there is an open connection we will immediately start to 
             * listen for incoming data.
             * 
             */

            _state.EndPoint = endpoint;

            _state.Socket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            bool isConnected = await Task.Factory.FromAsync(
                (callback, state) => _state.Socket.BeginConnect(_state.EndPoint, new AsyncCallback(callback), state),
                (arg) => {
                    try
                    {
                        _state.Socket.EndConnect(arg);

                        if (useSsl)
                        {
                            string target = "";

                            if (endpoint is DnsEndPoint)
                            {
                                target = ((DnsEndPoint)endpoint).Host;
                            }
                            else if (endpoint is IPEndPoint)
                            {
                                target = ((IPEndPoint)endpoint).Address.ToString();
                            }
                            else
                            {
                                throw new ArgumentException(nameof(endpoint));
                            }

                            _state.Stream = new SslStream(new NetworkStream(_state.Socket), true, (sender, certificate, chain, sslPolicyErrors) => {
                                if (sslPolicyErrors == SslPolicyErrors.None) return true;

                                if (allowCertificateChainErrors && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors) {
                                    System.Diagnostics.Trace.TraceWarning("Remote certificate chain errors are allowed and observed. It's STRONGLY RECOMMENDED to disallow chain errors in production!");
                                    return true;
                                }
                                
                                return false;
                            });

                            // ToDo: Authenticate async
                            try
                            {
                                // These errors can be mostly ignored but the connection should be closed
                                ((SslStream)_state.Stream).AuthenticateAsClient(target);
                            } catch {
                                Close();
                            }
                        }
                        else
                        {
                            _state.Stream = new NetworkStream(_state.Socket);
                        }

                        // Incoke the event to let the world know a connection has been made
                        Common.InvokeEvent(this, Connected, new ConnectedEventArgs(_state.EndPoint));

                        // Start listening for new data
                        _state.Stream.BeginRead(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, new AsyncCallback(ReadCallback), _state);
                        
                    } 
                    catch (ArgumentException) {
                        throw;
                    }
                    catch (Exception ex) {
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                        return false;
                    }
                    
                    return true;
                },
                null);

            return isConnected;
        }

        /// <summary>
        /// Close the connection to the remote endpoint
        /// </summary>
        public void Close()
        {
            try
            {
                if (_state.Socket.IsConnected())
                {
                    _state.Socket.Shutdown(SocketShutdown.Both);
                    _state.Socket.Disconnect(false);
                } else {
                    _state.Stream.Dispose();
                    _state.Socket.Dispose();
                    _state.Stream = null;

                    Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(_state.EndPoint));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                // Do not rethrow the error when trying to close a closed socket.
            }
        }

        /// <summary>
        /// Send the specified message.
        /// </summary>
        /// <returns>Boolean value indicating whether the message has been sent</returns>
        /// <param name="message">Message.</param>
        public async Task<bool> Send(string message)
        {
            return await Send(_state.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Send the specified data.
        /// </summary>
        /// <returns>Boolean value indicating whether the data has been sent</returns>
        /// <param name="data">Data.</param>
        public async Task<bool> Send(byte[] data)
        {
            if (_state == null || !_state.Socket.IsConnected()) return false;

            int dataLength = data.Length;

            return await Task.Factory.FromAsync(
                (callback, state) => _state.Stream.BeginWrite(data, 0, dataLength, new AsyncCallback(callback), state),
                (arg) =>
                {
                    try
                    {
                        _state.Stream.EndWrite(arg);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError(ex.ToString());
                        return false;
                    }
                }, null);
        }

        void ReadCallback(IAsyncResult result)
        {
            /*
             * The ReceiveCallback method makes sure incoming data is being read
             * and that events are being fired to notify subscribers.
             */

            int bytesRead = _state.Stream.EndRead(result);

            if (bytesRead > 0)
            {
                byte[] received = new byte[bytesRead];
                Array.Copy(_state.ReceiveBuffer, received, bytesRead);

                Common.InvokeEvent(this, Received, new ReceivedEventArgs(_state.EndPoint, received, _state.Encoding));
            }

            if (!_state.Socket.IsConnected())
            {
                _state.Stream.Dispose();
                _state.Socket.Dispose();
                _state.Stream = null;

                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(_state.EndPoint));

                return;
            }

            _state.Stream.BeginRead(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, new AsyncCallback(ReadCallback), null);
        }
    }
}
