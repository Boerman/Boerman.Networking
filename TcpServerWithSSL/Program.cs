using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Boerman.Networking;

namespace TcpServerWithSSL
{
    class Program
    {
        /*
         * This sample shows how a TCP server instance with SSL/TLS support
         * works. When the certificate is provided the TCP server automatically
         * switches to SSL mode. Please note that it's not possible to use plain
         * TCP connections anymore. The certificate provided in this repo is a
         * self signed certificate for "127.0.0.1" which does not validate
         * against any CA and therefore produces certificate chain errors. In
         * order to connect with this server it's required to allow certificate
         * chain errors (NOT recommended in production). To see how the client
         * works with an SSL connection see the TcpClientWithSSL project.
         */
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
