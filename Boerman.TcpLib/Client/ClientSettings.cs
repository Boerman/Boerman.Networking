using System.Net;

namespace Boerman.TcpLib.Client
{
    public class ClientSettings
    {
        public EndPoint EndPoint { get; set; }
        public string Splitter { get; set; }
        public int Timeout { get; set; }
        public bool ReconnectOnDisconnect { get; set; }
    }
}