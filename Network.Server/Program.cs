using LiteNetLib;
using Network.Common.Server;
using NLog;
using System.Net;
using System.Threading;

namespace Network.Server
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var autoServer = new NetServerManager();
            autoServer.ServerMessage += NetServer_ServerMessage;
            autoServer.CreateServer(IPAddress.Any, 23456, 9050);

            while (true)
            {
                autoServer.DoEvents();
                Thread.Sleep(1);
            }

            autoServer.Destroy();
        }

        private static void NetServer_ServerMessage(NetPeer client, string message)
        {
            Logger.Info($"{client.EndPoint.Address}: {message}");
        }
    }
}
