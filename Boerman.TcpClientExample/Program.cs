﻿using System;
using System.Net;
using System.Threading.Tasks;

namespace Boerman.TcpClientExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new Boerman.Networking.TcpClient();

            client.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.EndPoint} connected");
            };

            client.Received += (sender, e) =>
            {
                Console.Write(e.Data);
            };

            client.Disconnected += (sender, e) =>
            {
                Console.WriteLine($"{e.TimeStamp}: {e.EndPoint} disconnected");
            };

            while (!await client.Open(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626))) {
                Console.WriteLine($"{DateTime.UtcNow}: Trying to connect...");
            }

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
                await client.Send(key.KeyChar.ToString());
            } while (key.Key != ConsoleKey.Escape);
        }
    }
}
