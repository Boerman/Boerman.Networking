using System;
using System.Net;
using System.Threading;

namespace Boerman.TcpClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a new TcpServer listening on port 2626
            var client = new TcpLib.Client.TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626));

            client.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} connected");
            };

            client.PartReceived += (sender, e) =>
            {
                Console.Write(e.Data);
            };

            client.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} disconnected");
            };

            client.Open();

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();

                client.Send(key.KeyChar.ToString());
            } while (key.Key != ConsoleKey.Escape);
        }
    }
}
