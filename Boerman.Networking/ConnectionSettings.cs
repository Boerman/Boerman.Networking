using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Boerman.Networking
{
    internal class ConnectionSettings
    {
        public EndPoint EndPoint { get; set; }
        public Encoding Encoding { get; set; }
        public bool UseSsl { get; set; }
        public X509Certificate2 Certificate { get; set; }
    }
}