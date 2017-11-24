using System;
using System.IO;
using System.Net;

namespace Boerman.TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Boerman.Networking.TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2626));

            server.Connected += (sender, e) => {
                Console.WriteLine($"{e.TimeStamp}: {e.Endpoint} connected");
            };

            Stream stream = new FileStream("./file.mp4", FileMode.Create, FileAccess.Write);
            int byteCounter = 0;
            server.DataReceived += (sender, e) =>
            {
                stream.Write(e.Bytes, 0, e.Bytes.Length);
                stream.Flush();
                byteCounter += e.Bytes.Length;
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

            server.Stop();
        }
    }
}
