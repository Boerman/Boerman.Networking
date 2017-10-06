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
                catch (ObjectDisposedException)
                {
                    if (param.AsyncState is StateObject)
                    {
                        // TcpClient is already closed. All we gotta do is remove all references. (Aaand the garbage man will clean the shit behind our backs.)
                        StateObject state = (StateObject) param.AsyncState;

                        _handlers.TryRemove(state.Guid, out StateObject stateObject);

                        InvokeOnDisconnectEvent(stateObject.Endpoint);
                    }
                }
                catch (InvalidOperationException)
                {
                    if (param.AsyncState is StateObject)
                    {
                        // Tcp client isn't connected (We'd better clean up the resources.)
                        StateObject state = (StateObject) param.AsyncState;
                        state.Socket.Dispose();

                        _handlers.TryRemove(state.Guid, out StateObject stateObject);

                        InvokeOnDisconnectEvent(stateObject.Endpoint);
                    }
                }
                catch (SocketException ex)
                {
                    switch (ex.NativeErrorCode)
                    {
                        case 10054: // Force disconnect
                            if (param.AsyncState is StateObject)
                            {
                                var state = (StateObject) param.AsyncState;
                                state.Socket.Dispose();

                                StateObject stateObject;
                                _handlers.TryRemove(state.Guid, out stateObject);

                                InvokeOnDisconnectEvent(stateObject.Endpoint);
                            }
                            break;
                    }
                }
                catch (NullReferenceException)
                {
                    // If this happens we're really far away! Restart the socket listener!
                    Restart();
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);        // MAYDAY MAYDAY MAYDAY
            }
        }
    }
}
