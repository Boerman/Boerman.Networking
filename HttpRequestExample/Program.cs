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

            if (await client.Open(new DnsEndPoint("google.com", 80)))
            {
                while (true) {
                    var s = Console.ReadKey();
                    await client.Send(s.KeyChar.ToString());
                }

                //await client.Send(@"GET / HTTP/1.1
                                    //Host: google.com
                                    //User-Agent: custom
                                    //Accept: */*
                                    //");
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Connection could not be made");
                Console.ResetColor();
            }

            Console.ReadKey();
        }
    }
}
