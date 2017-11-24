using System.Net;
using System.Text;

namespace Boerman.Networking
{
    internal class ConnectionSettings
    {
        public EndPoint EndPoint { get; set; }
        public Encoding Encoding { get; set; }
    }
}