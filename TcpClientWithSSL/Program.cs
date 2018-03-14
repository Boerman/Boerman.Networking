using System;
using System.Net;
using System.Threading.Tasks;
using Boerman.Networking;

namespace TcpClientWithSSL
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tcpClient = new TcpClient();

            tcpClient.Connected += (sender, e) => {
                Console.WriteLine("- Connected");
            };

            tcpClient.Received += (sender, e) => {
                Console.WriteLine($"- Received {e.Bytes.Length} bytes");
            };

            tcpClient.Disconnected += (sender, e) => {
                Console.WriteLine("- Disconnected");
            };

            await tcpClient.Open(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626), 
                                 useSsl: true, 
                                 allowCertificateChainErrors: true);

            while (true) {
                await tcpClient.Send(Console.ReadKey().KeyChar.ToString());
            }
        }
    }
}
