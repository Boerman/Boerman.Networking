using System.Net;
using System.Text;

namespace Boerman.Networking
{
    public class ConnectionSettings
    {
        public EndPoint EndPoint { get; set; }
        public Encoding Encoding { get; set; }
    }
}