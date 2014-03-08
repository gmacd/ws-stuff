using Alchemy;
using Alchemy.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WsBackend
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("WS Server Started.  Any key to quit.");
            StartServer();
            Console.ReadKey();
        }

        static void StartServer()
        {
            var server = new Server();

            const int port = 81;
            var wsServer = new WebSocketServer(port, IPAddress.Any)
            {
                OnConnect = ctx => server.OnConnect(ctx),
                OnConnected = ctx => server.OnConnected(ctx),
                OnDisconnect = ctx => server.OnDisconnect(ctx),
                OnSend = ctx => server.OnSend(ctx),
                OnReceive = ctx => server.OnReceive(ctx),
            };

            wsServer.Start();
        }
    }
}
