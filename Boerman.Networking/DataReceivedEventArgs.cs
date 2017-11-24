using System.Net;
using System.Text;

namespace Boerman.Networking
{
    public class DataReceivedEventArgs
    {
        public DataReceivedEventArgs(EndPoint endpoint, byte[] data) {
            Endpoint = endpoint;
            Bytes = data;
        }

        public byte[] Bytes { get; }
        public string Data => Encoding.GetEncoding("utf-8").GetString(Bytes, 0, Bytes.Length);
        public EndPoint Endpoint;
    }
}