using System;
using System.Net;
using System.Threading.Tasks;

namespace Boerman.TcpServerExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var server = new Boerman.Networking.TcpServer(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 2626));

            server.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.EndPoint} connected");
            };

            server.Received += (sender, e) =>
            {
                Console.Write(e.Data);
            };

            server.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"{e.TimeStamp}: {e.EndPoint} disconnected");
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
