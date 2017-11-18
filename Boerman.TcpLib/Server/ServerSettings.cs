using System.Net;
using System.Text;

namespace Boerman.TcpLib.Server
{
    public class ServerSettings
    {
        public IPEndPoint   IpEndPoint      { get; set; }
        public string       Splitter        { get; set; }
        public int          ClientTimeout   { get; set; }
        public bool         ReuseAddress    { get; set; }
        public Encoding     Encoding        { get; set; }
    }
}