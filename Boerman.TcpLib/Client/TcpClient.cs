﻿/*
 * ToDo: Add default configuration for properties (not on class constructor level)
 */

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Boerman.Core;
using Boerman.Core.Serialization;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    public partial class TcpClient<TSend, TReceive> 
        where TSend : class
        where TReceive : class
    {
        private StateObject _state;
        
        private readonly ClientSettings _clientSettings;

        private readonly ManualResetEvent _isConnected = new ManualResetEvent(false);
        private readonly ManualResetEvent _isSending = new ManualResetEvent(false);
        
        private bool _isShuttingDown;
        
        public TcpClient(IPEndPoint endpoint)
        {
            _clientSettings = new ClientSettings
            {
                EndPoint = endpoint,
                Splitter = "\r\n",
                Timeout = 1020000,
                ReconnectOnDisconnect = false
            };
        }

        public TcpClient(ClientSettings settings)
        {
            _clientSettings = settings;
        }

        /// <summary>
        /// Open the connection to a remote endpoint
        /// </summary>
        public void Open()
        {
            try
            {
                // Reset the variables which are set earlier
                _isConnected.Reset();
                _isSending.Reset();

                // Continue trying until there's a connection.
                bool success;
                
                do
                {
                    _state = new StateObject(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                    _state.Socket.BeginConnect(_clientSettings.EndPoint, ConnectCallback, _state);

                    success = _isConnected.WaitOne(10000);
                } while (!success);

                // We are connected!

                _isShuttingDown = false;

                InvokeOnConnectEvent(_clientSettings.EndPoint);
                
                _state.Socket.BeginReceive(_state.ReceiveBuffer, 0, _state.ReceiveBufferSize, 0, ReceiveCallback,
                    _state);
            }
            catch (SocketException ex)
            {
                switch (ex.NativeErrorCode)
                {
                    case 10054: // An existing connection was forcibly closed by the remote host
                        if (_clientSettings.ReconnectOnDisconnect)
                        {
                            Close();
                            Open();
                        }
                        break;
                    default:
                        throw;
                }
            }
        }

        /// <summary>
        /// Close the connection to a remote endpoint
        /// </summary>
        public void Close()
        {
            try
            {
                _isShuttingDown = true;

                // There's no specific reason to set a timeout as this operation
                // should be completed pretty fast anyway.
                _isSending.WaitOne();
                _isSending.Reset();
                
                _isConnected.Reset();

                _state.Socket.Shutdown(SocketShutdown.Both);
                _state.Socket.Disconnect(false);
                _state.Socket.Dispose();

                InvokeOnDisconnectEvent(_clientSettings.EndPoint);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10038)
                {
                    // Something is done on not a socket... 
                }
                else if (ex.ErrorCode == 10004)
                {
                    // Some blocking call was interrupted.
                }
                else
                {
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
                // We can just ignore this one :)
            }
        }

        public void Send(string message)
        {
            Send(Encoding.GetEncoding(Constants.Encoding).GetBytes(message));
        }

        public void Send(TSend data)
        {
            var splitter = Encoding.GetEncoding(Constants.Encoding).GetBytes(_clientSettings.Splitter);
            var array = ObjectSerializer.Serialize(data).Concat(splitter).ToArray();

            Send(array);
        }

        private void Send(byte[] data)
        {
            // Wait with the send process until we're connected. (ToDo: Check whether we have to add some timeout)
            _isConnected.WaitOne();

            _state.OutboundMessages.Enqueue(data);
            
            if (!_isSending.WaitOne(0))
                EmptyOutboundQueue();   // We have to initialize the sending process.
        }

        private void EmptyOutboundQueue()
        {
            while (_state.OutboundMessages.Any())
            {
                if (_isShuttingDown)
                {
                    // Empty queue and return
                    while (_state.OutboundMessages.Any())
                    {
                        byte[] removedData;
                        _state.OutboundMessages.TryDequeue(out removedData);
                    }

                    return;
                }
                
                _state.OutboundMessages.TryDequeue(out _state.SendBuffer);

                try
                {
                    // We can only send one message at a time.
                    _state.Socket.BeginSend(_state.SendBuffer, 0, _state.SendBuffer.Length, 0, SendCallback,
                        _state);
                }
                catch (SocketException ex)
                {
                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // An existing connection was forcibly closed by the remote host
                            _isSending.Set(); // Otherwise the program will wait indefinitely.

                            if (_clientSettings.ReconnectOnDisconnect)
                            {
                                Close();
                                Open();
                            }
                            break;
                        default:
                            throw;
                    }
                }

                // Wait until we're cleared to send another message
                _isSending.WaitOne();
            }
        }
    }

    public class TcpClient : TcpClient<string, string>
    {
        public TcpClient(IPEndPoint endpoint) : base(endpoint)
        {
            
        }

        public TcpClient(ClientSettings settings) : base (settings)
        {
            
        }
    }
}
