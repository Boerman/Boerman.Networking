using System;
using System.Net.Sockets;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Client
{
    partial class TcpClient<TSend, TReceive>
    {
        public void ExecuteFunction(Action<IAsyncResult> action, IAsyncResult param)
        {
            try
            {
                try
                {
                    action(param);
                }
                catch (ObjectDisposedException)
                {
                    // I guess theh object should've been disposed. Try it.
                    // Tcp client is already closed! Start it again.
                    Open();
                }
                catch (SocketException ex)
                {
                    StateObject state = param.AsyncState as StateObject;

                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // An existing connection was forcibly closed by the remote host
                            if (_clientSettings.ReconnectOnDisconnect)
                            {
                                Close();
                                Open();
                            }
                            break;
                        case 10061:
                            // No connection could be made because the target machine actively refused it. Do nuthin'
                            // Usually the tool will try to reconnect every 10 seconds or so.
                            break;
                        default:

                            break;
                    }
                }
                catch (Exception)
                {
                    // Tcp client isn't connected (We'd better clean up the resources.)
                    StateObject state = param.AsyncState as StateObject;
                    
                    state?.Socket.Dispose();
                    
                    Open();
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);    // Aaand hopefully the service will be restarted.
            }
        }
    }
}
