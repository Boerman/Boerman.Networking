using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class OnReceiveEventArgs<T>
    {
        public OnReceiveEventArgs(T data, EndPoint endpoint)
        {
            Endpoint = endpoint;
            Data = data;
        }

        public T Data { get; }
        public EndPoint Endpoint;
    }
}