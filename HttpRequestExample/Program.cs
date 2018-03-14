using System;
using System.Net;
using System.Threading.Tasks;
using Boerman.Networking;

namespace HttpRequestExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new TcpClient();

            client.Connected += (sender, e) => {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Connected");
                Console.ResetColor();
            };

            client.Received += (sender, e) => {
                Console.Write(e.Data);
            };

            client.Disconnected += (sender, e) => {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Disconnected");
                Console.ResetColor();
            };

            if (await client.Open(new DnsEndPoint("google.com", 443), true))
            {
                await client.Send("GET / HTTP/1.1\nHost: google.com\n\n");
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Connection could not be made");
                Console.ResetColor();
            }

            Console.ReadKey();
            client.Close();
        }
    }
}
