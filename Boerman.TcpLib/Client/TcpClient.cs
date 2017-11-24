using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Boerman.TcpLib.Server;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    public class TcpClient
    {
        StateObject _state;
        
        readonly ConnectionSettings _settings;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public TcpClient(EndPoint endpoint)
        {
            _settings = new ConnectionSettings()
            {
                EndPoint = endpoint,
                Timeout = new TimeSpan(0, 0, 30),
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpClient(ConnectionSettings settings)
        {
            _settings = settings;
        }

        void ExecuteFunction(Action<IAsyncResult> action, IAsyncResult param)
        {
            try
            {
                try
                {
                    action(param);
                }
                catch (SocketException ex)
                {
                    StateObject state = param.AsyncState as StateObject;

                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // An existing connection was forcibly closed by the remote host
                            Close();
                            break;
                        case 10061:
                            // No connection could be made because the target machine actively refused it. Do nuthin'
                            // Usually the tool will try to reconnect every 10 seconds or so.
                            break;
                        default:

                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());

                    Close();
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Open the connection to a remote endpoint
        /// </summary>
        public async Task<bool> Open()
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

            // ToDo: Instantiate a new StateObject in a somewhat more fancy way.
            _state = new StateObject(new Socket(
                AddressFamily.InterNetwork, 
                SocketType.Stream, 
                ProtocolType.Tcp));

            bool isConnected = await Task.Factory.FromAsync(
                (callback, state) => {
                    return _state.Socket.BeginConnect(
                        _settings.EndPoint, 
                        new AsyncCallback(callback), state);
                },
                (arg) => {
                    try
                    {
                        _state.Socket.EndConnect(arg);
                        
                        // Incoke the event to let the world know a connection
                        // has been made. 
                        Common.InvokeEvent(
                            this, 
                            Connected, 
                            new ConnectedEventArgs(_settings.EndPoint));
                        
                        // Start listening for new data
                        _state.Socket.BeginReceive(
                            _state.ReceiveBuffer, 
                            0, 
                            _state.ReceiveBufferSize, 
                            0, 
                            new AsyncCallback(ReceiveCallback),
                            _state);
                    
                    } catch (Exception ex) {
                        // ToDo: return more detailed error information about why a connection could not be made
                        System.Diagnostics.Trace.TraceError(ex.ToString());

                        return false;
                    }

                    return true;
                },
                null);  // The state can be null as we're in a pretty local context. No errors either.

            return isConnected;
        }

        /// <summary>
        /// Close the connection to a remote endpoint
        /// </summary>
        public void Close()
        {
            try
            {
                if (_state.Socket.IsConnected())
                {
                    _state.Socket.Shutdown(SocketShutdown.Both);
                    _state.Socket.Disconnect(false);
                }

                _state.Socket.Dispose();
                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(_settings.EndPoint));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Send the specified message.
        /// </summary>
        /// <param name="message">Message</param>
        public void Send(string message)
        {
            Send(_settings.Encoding.GetBytes(message));
        }

        /// <summary>
        /// Send the specified data.
        /// </summary>
        /// <param name="data">Data</param>
        public void Send(byte[] data)
        {
            if (_state == null || !_state.Socket.IsConnected()) {
                throw new Exception("Connection is not open");  // ToDo: return some information about the status of the operation (even if it's just a bool or something)
            }

            _state.Socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), _state);
        }

        //void ConnectCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        StateObject state = (StateObject)ar.AsyncState;
        //        state.Socket.EndConnect(ar);

        //        Common.InvokeEvent(this, Connected, new ConnectedEventArgs(_settings.EndPoint));

        //        _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, new AsyncCallback(ReceiveCallback),
        //            _state);
        //    } catch (Exception ex) {
                
        //    }
        //}

        void SendCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate (IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;
                int bytesSend = state.Socket.EndSend(result);

                _state.LastSend = DateTime.UtcNow;
            }, ar);
        }

        void ReceiveCallback(IAsyncResult ar)
        {
            ExecuteFunction(delegate (IAsyncResult result)
            {
                StateObject state = (StateObject)result.AsyncState;

                Socket handler = state.Socket;

                int bytesRead = handler.EndReceive(result);

                if (!handler.IsConnected())
                {
                    Close();
                    return;
                }

                byte[] received = state.ReceiveBuffer;
                handler.BeginReceive(state.ReceiveBuffer, 0, state.ReceiveBufferSize, 0, new AsyncCallback(ReceiveCallback), state);

                Common.InvokeEvent(
                    this, 
                    DataReceived, 
                    new DataReceivedEventArgs(state.Endpoint, received));

                _state.LastReceived = DateTime.UtcNow;
            }, ar);
        }
    }
}
