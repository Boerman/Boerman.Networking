using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Boerman.Networking;

namespace TcpServerWithSSL
{
    class Program
    {
        static void Main(string[] args)
        {
            var tcpServer = new TcpServer(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2626), new X509Certificate2("merged.pfx", "1234"));

            tcpServer.Connected += (sender, e) => {
                Console.WriteLine($"{e.EndPoint.ToString()}: connected");
            };

            tcpServer.Received += (sender, e) => {
                Console.WriteLine($"{e.EndPoint.ToString()}: {e.Bytes.Length} bytes");
            };

            tcpServer.Disconnected += (sender, e) => {
                Console.WriteLine($"{e.EndPoint.ToString()}: disconnected");
            };

            tcpServer.Start();

            Console.ReadKey();
            tcpServer.Stop();
        }
    }
}
