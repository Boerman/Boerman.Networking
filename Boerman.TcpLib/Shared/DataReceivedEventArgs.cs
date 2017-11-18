using System.Net;

namespace Boerman.TcpLib.Shared
{
    public class DataReceivedEventArgs
    {
        public DataReceivedEventArgs(string data, EndPoint endpoint)
        {
            Endpoint = endpoint;
            Data = data;
        }

        public string Data { get; }
        public EndPoint Endpoint;
    }
}