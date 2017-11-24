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

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ConnectedEventArgs> Connected;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public TcpClient()
        {
            _state = new StateObject();
        }

        public TcpClient(Encoding encoding) {
            _state = new StateObject()
            {
                Encoding = encoding
            };
        }


        /// <summary>
        /// Open the connection to the remote endpoint.
        /// </summary>
        /// <returns>Boolean indicating whether the connection is open or not</returns>
        public async Task<bool> Open(EndPoint endpoint)
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
                        
                        // Incoke the event to let the world know a connection has been made
                        Common.InvokeEvent(this, Connected, new ConnectedEventArgs(_state.EndPoint));
                        
                        // Start listening for new data
                        _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, new AsyncCallback(ReadCallback), _state);
                    
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

                Common.InvokeEvent(this, Disconnected, new DisconnectedEventArgs(_state.EndPoint));
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

            int bytesSent = await Task.Factory.FromAsync(
                (callback, state) => _state.Socket.BeginSend(data, 0, dataLength, 0, new AsyncCallback(callback), state),
                (arg) =>
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

        void ReadCallback(IAsyncResult result)
        {
            /*
             * The ReceiveCallback method makes sure incoming data is being read
             * and that events are being fired to notify subscribers.
             */

            if (!_state.Socket.IsConnected())
            {
                Close();
                return;
            }

            int bytesRead = _state.Socket.EndReceive(result);

            byte[] received = new byte[bytesRead];
            Array.Copy(_state.ReceiveBuffer, received, bytesRead);

            Common.InvokeEvent(this, DataReceived, new DataReceivedEventArgs(_state.EndPoint, received));

            _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, new AsyncCallback(ReadCallback), null);
        }
    }
}
