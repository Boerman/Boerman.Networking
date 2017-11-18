using System;
using System.Net;
using System.Threading;

namespace Boerman.TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new TcpServer listening on port 2626
            var server = new TcpLib.Server.TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626));

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

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                server.Send(key.KeyChar.ToString());
            } while (key.Key != ConsoleKey.Escape);
        }
    }
}
