using System;
using System.Net;

namespace Boerman.TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new TcpLib.Server.TcpServer(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2626));

            server.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} connected");
            };

            server.PartReceived += (sender, e) =>
            {
                Console.Write(e.Data);
            };

            server.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} disconnected");
            };

            server.Start();

            Console.ReadKey();
        }
    }
}
