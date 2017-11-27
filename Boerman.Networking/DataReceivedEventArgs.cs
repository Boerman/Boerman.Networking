using System.Net;
using System.Text;

namespace Boerman.Networking
{
    public class DataReceivedEventArgs
    {
        internal DataReceivedEventArgs(EndPoint endpoint, byte[] data) {
            EndPoint = endpoint;
            Bytes = data;
            Encoding = Encoding.GetEncoding("utf-8");
        }

        internal DataReceivedEventArgs(EndPoint endpoint, byte[] data, Encoding encoding) {
            EndPoint = endpoint;
            Bytes = data;
            Encoding = encoding;
        }

        private Encoding Encoding { get; }

        public byte[] Bytes { get; }
        public string Data => Encoding.GetString(Bytes, 0, Bytes.Length);
        public EndPoint EndPoint;
    }
}
