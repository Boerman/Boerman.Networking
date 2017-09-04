using System;
using System.Net.Sockets;
using Boerman.TcpLib.Shared;

namespace Boerman.TcpLib.Server
{
    partial class TcpServer<TSend, TReceive>
        where TSend : class
        where TReceive : class
    {
        /// <summary>
        /// This function executes the given function and handles exceptions, if any.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="param">The param with which the action has to be executed.</param>
        /// <returns></returns>
        public void ExecuteFunction(Action<IAsyncResult> action, IAsyncResult param)
        {
            try
            {
                try
                {
                    action(param);
                }
                catch (ObjectDisposedException ex)
                {
                    //Logger.Warn(ex);
                    
                    if (param.AsyncState is StateObject)
                    {
                        // TcpClient is already closed. All we gotta do is remove all references. (Aaand the garbage man will clean the shit behind our backs.)
                        StateObject state = (StateObject) param.AsyncState;
                        
                        _handlers.TryRemove(state.Guid, out StateObject stateObject);

                        RaiseDisconnectEvent(stateObject);
                        RaiseExceptionEvent(stateObject);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    //Logger.Warn(ex);
                    
                    if (param.AsyncState is StateObject)
                    {
                        // Tcp client isn't connected (We'd better clean up the resources.)
                        StateObject state = (StateObject) param.AsyncState;
                        state.WorkSocket.Dispose();
                        
                        _handlers.TryRemove(state.Guid, out StateObject stateObject);

                        RaiseDisconnectEvent(stateObject);
                        RaiseExceptionEvent(stateObject);
                    }
                }
                catch (SocketException ex)
                {
                    //Logger.Warn(ex);

                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // Force disconnect
                            //if (param.AsyncState is Socket)   // This is the main socket we use to listen to incoming connections. We're screwed if we kill this one.

                            if (param.AsyncState is StateObject)
                            {
                                var state = (StateObject) param.AsyncState;
                                state.WorkSocket.Dispose();

                                StateObject stateObject;
                                _handlers.TryRemove(state.Guid, out stateObject);

                                RaiseDisconnectEvent(stateObject);
                                RaiseExceptionEvent(stateObject);
                            }
                            break;
                        default:

                            break;
                    }
                }
                catch (NullReferenceException ex)
                {
                    //Logger.Fatal(ex);
                    
                    // If this happens we're really far away! Restart the socket listener!
                    Restart();
                }
                catch (Exception ex)
                {
                    //Logger.Error(ex);
                }
            }
            catch (Exception ex)
            {
                //Logger.Fatal(ex);

                Environment.Exit(1);        // MAYDAY MAYDAY MAYDAY
            }
        }
    }
}
