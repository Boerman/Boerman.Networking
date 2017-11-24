using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Boerman.TcpClientExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a new TcpServer listening on port 2626
            var client = new TcpLib.Client.TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626));
            //var client = new TcpLib.Client.TcpClient(new DnsEndPoint("google.com", 80)); // Add some http parser and we got ourselves a webclient

            client.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} connected");
            };

            client.DataReceived += (sender, e) =>
            {
                Console.Write(e.Data);
            };

            client.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} disconnected");
            };

            if (!await client.Open()) {
                Console.WriteLine("Not open");
            } else {
                Console.WriteLine("Open");
            }

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                client.Send(key.KeyChar.ToString());
            } while (key.Key != ConsoleKey.Escape);
        }
    }
}
