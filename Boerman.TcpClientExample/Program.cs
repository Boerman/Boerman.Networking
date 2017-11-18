using System;
using System.Net;

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

            server.Start();

            Console.ReadKey();
        }
    }
}
