using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class DataReceivedEventArgs<T>
    {
        public DataReceivedEventArgs(T data, EndPoint endpoint)
        {
            Endpoint = endpoint;
            Data = data;
        }

        public T Data { get; }
        public EndPoint Endpoint;
    }
}