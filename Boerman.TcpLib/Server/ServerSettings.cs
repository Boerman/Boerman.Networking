using System.Net;

namespace Boerman.TcpLib.Server
{
    public class ServerSettings
    {
        public IPEndPoint   IpEndPoint      { get; set; }
        public string       Splitter        { get; set; }
        public int          ClientTimeout   { get; set; }
        public bool         DontLinger      { get; set; }
        public bool         ReuseAddress    { get; set; }
    }
}