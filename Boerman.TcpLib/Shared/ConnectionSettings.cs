using System;
using System.Net;
using System.Text;

namespace Boerman.TcpLib.Server
{
    public class ConnectionSettings
    {
        public EndPoint EndPoint { get; set; }
        public TimeSpan Timeout { get; set; }
        public Encoding Encoding { get; set; }
    }
}