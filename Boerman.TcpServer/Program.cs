using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Boerman.TcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new Boerman.Networking.TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626));

            server.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} connected");
            };

            server.DataReceived += (sender, e) =>
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
                await server.Send(key.KeyChar.ToString());
            } while (key.Key != ConsoleKey.Escape);

            server.Stop();
        }
    }
}
