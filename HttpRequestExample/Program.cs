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
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Making HTTP request, close connection immediately:\n\n");
            Console.ResetColor();
            await MakeHttpRequest(true);

            System.Threading.Thread.Sleep(2000);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Making HTTPS request, close connection immediately:\n\n");
            Console.ResetColor();

            // You'll see a brief moment here where the connection is open. This
            // is due to the SSL handshake being made.
            await MakeHttpsRequest(true);

            System.Threading.Thread.Sleep(2000);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Making HTTP request:\n\n");
            Console.ResetColor();
            await MakeHttpRequest(false);

            System.Threading.Thread.Sleep(2000);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("Making HTTPS request:\n\n");
            Console.ResetColor();
            await MakeHttpsRequest(false);

            Console.ReadKey();
        }

        public static async Task MakeHttpRequest(bool closeConnection) {
            var client = GetClient();

            if (await client.Open(new DnsEndPoint("google.com", 80)))
            {
                await client.Send("GET / HTTP/1.1\nHost: google.com\n\n");
                if (closeConnection) client.Close();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Connection could not be made");
                Console.ResetColor();
            }
        }

        public static async Task MakeHttpsRequest(bool closeConnection) {
            var client = GetClient();

            if (await client.Open(new DnsEndPoint("google.com", 443), true))
            {
                await client.Send("GET / HTTP/1.1\nHost: google.com\n\n");
                if (closeConnection) client.Close();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Connection could not be made");
                Console.ResetColor();
            }
        }

        public static TcpClient GetClient()
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

            return client;
        }
    }
}
