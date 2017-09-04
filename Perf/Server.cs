using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Perf
{
    public class Server
    {
        private List<xConnection> _sockets;
        private Socket _serverSocket;

        private int _port = 8888;

        public Server()
        {
        }

		public bool Start()
		{
			System.Net.IPHostEntry localhost = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
			System.Net.IPEndPoint serverEndPoint;
			
            try
			{
				serverEndPoint = new System.Net.IPEndPoint(localhost.AddressList[0], _port);
			}
			catch (ArgumentOutOfRangeException e)
			{
                // ToDo: add handling logic to handle binding stuff to ports < 1024 (webservers etc)
				throw new ArgumentOutOfRangeException("Port number entered would seem to be invalid, should be between 1024 and 65000", e);
			}

			try
			{
				_serverSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			}
			catch (System.Net.Sockets.SocketException e)
			{
				throw new ApplicationException("Could not create socket, check to make sure not duplicating port", e);
			}

			try
			{
				_serverSocket.Bind(serverEndPoint);
				_serverSocket.Listen(_backlog);
			}
			catch (Exception e)
			{
				throw new ApplicationException("Error occured while binding socket, check inner exception", e);
			}
			
            try
			{
				//warning, only call this once, this is a bug in .net 2.0 that breaks if 
				// you're running multiple asynch accepts, this bug may be fixed, but
				// it was a major pain in the ass previously, so make sure there is only one
				//BeginAccept running
				_serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
			}
			catch (Exception e)
			{
				throw new ApplicationException("Error occured starting listeners, check inner exception", e);
			}
			return true;
		}

		private void acceptCallback(IAsyncResult result)
		{
			xConnection conn = new xConnection();
			try
			{
				//Finish accepting the connection
				System.Net.Sockets.Socket s = (System.Net.Sockets.Socket)result.AsyncState;
				conn = new xConnection();
				conn.socket = s.EndAccept(result);
				conn.buffer = new byte[_bufferSize];
				lock (_sockets)
				{
					_sockets.Add(conn);
				}
				//Queue recieving of data from the connection
				conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
				//Queue the accept of the next incomming connection
				_serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
			}
			catch (SocketException e)
			{
				if (conn.socket != null)
				{
					conn.socket.Close();
					lock (_sockets)
					{
						_sockets.Remove(conn);
					}
				}
				//Queue the next accept, think this should be here, stop attacks based on killing the waiting listeners
				_serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
			}
			catch (Exception e)
			{
				if (conn.socket != null)
				{
					conn.socket.Close();
					lock (_sockets)
					{
						_sockets.Remove(conn);
					}
				}
				//Queue the next accept, think this should be here, stop attacks based on killing the waiting listeners
				_serverSocket.BeginAccept(new AsyncCallback(acceptCallback), _serverSocket);
			}
		}

		private void ReceiveCallback(IAsyncResult result)
		{
			//get our connection from the callback
			xConnection conn = (xConnection)result.AsyncState;
			//catch any errors, we'd better not have any
			try
			{
				//Grab our buffer and count the number of bytes receives
				int bytesRead = conn.socket.EndReceive(result);
				//make sure we've read something, if we haven't it supposadly means that the client disconnected
				if (bytesRead > 0)
				{
					//put whatever you want to do when you receive data here

					//Queue the next receive
					conn.socket.BeginReceive(conn.buffer, 0, conn.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), conn);
				}
				else
				{
					//Callback run but no data, close the connection
					//supposadly means a disconnect
					//and we still have to close the socket, even though we throw the event later
					conn.socket.Close();
					lock (_sockets)
					{
						_sockets.Remove(conn);
					}
				}
			}
			catch (SocketException e)
			{
				//Something went terribly wrong
				//which shouldn't have happened
				if (conn.socket != null)
				{
					conn.socket.Close();
					lock (_sockets)
					{
						_sockets.Remove(conn);
					}
				}
			}
		}

		public bool Send(byte[] message, xConnection conn)
		{
			if (conn != null && conn.socket.Connected)
			{
				lock (conn.socket)
				{
					//we use a blocking mode send, no async on the outgoing
					//since this is primarily a multithreaded application, shouldn't cause problems to send in blocking mode
					conn.socket.Send(message, message.Length, SocketFlags.None);
				}
			}
			else
				return false;
			return true;
		}
    }
}
