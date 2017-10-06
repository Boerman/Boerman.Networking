﻿using System;
using System.Net.Sockets;

namespace Boerman.TcpLib.Shared
{
    public static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)         { return false; }
            catch (ObjectDisposedException) { return false; }
        }
    }
}
