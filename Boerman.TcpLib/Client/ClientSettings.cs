using System.Net;
using System.Text;

namespace Boerman.TcpLib.Client
{
    public class ClientSettings
    {
        public EndPoint     EndPoint                { get; set; }
        public bool         ReconnectOnDisconnect   { get; set; }
        public Encoding     Encoding                { get; set; }
    }
}