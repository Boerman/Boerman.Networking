using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Boerman.Networking
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
                Encoding = Encoding.GetEncoding("utf-8")
            };
        }

        public TcpClient(ConnectionSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Open the connection to the remote endpoint.
        /// </summary>
        /// <returns>Boolean indicating whether the connection is open or not</returns>
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
                    return _state.Socket.BeginConnect(_settings.EndPoint, new AsyncCallback(callback), state);
                }, (arg) => {
                    try
                    {
                        _state.Socket.EndConnect(arg);
                        
                        // Incoke the event to let the world know a connection has been made
                        Common.InvokeEvent(this, Connected, new ConnectedEventArgs(_settings.EndPoint));
                        
                        // Start listening for new data
                        _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, new AsyncCallback(ReceiveCallback), _state);
                    
                    } catch (Exception ex) {
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
        /// <returns>Boolean value indicating whether the message has been sent</returns>
        /// <param name="message">Message.</param>
        public async Task<bool> Send(string message)
        {
            return await Send(_settings.Encoding.GetBytes(message));
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

            int bytesSent = await Task.Factory.FromAsync((callback, state) =>
            {
                return _state.Socket.BeginSend(data, 0, dataLength, 0, new AsyncCallback(callback), state);
            }, (arg) =>
            {
                try
                {
                    return _state.Socket.EndSend(arg);
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

        void ReceiveCallback(IAsyncResult result)
        {
            /*
             * The ReceiveCallback method makes sure incoming data is being read
             * and that events are being fired to notify subscribers.
             */

            int bytesRead = _state.Socket.EndReceive(result);

            if (!_state.Socket.IsConnected())
            {
                Close();
                return;
            }

            byte[] received = new byte[bytesRead];
            Array.Copy(_state.ReceiveBuffer, received, bytesRead);

            Common.InvokeEvent(this, DataReceived, new DataReceivedEventArgs(_state.Endpoint, received));

            _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, new AsyncCallback(ReceiveCallback), null);
        }
    }
}
