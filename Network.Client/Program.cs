using Network.Common.Client;
using NLog;
using System.Threading;

namespace Network.Client
{
    public class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static NetClientManager _autoClient;

        static void Main(string[] args)
        {
            // SockNet TCP socket client for recieving and sending packets to server.
            _autoClient = new NetClientManager(9050);
            _autoClient.ClientConnected += ClientNet_ClientConnected;
            _autoClient.ClientDisconnected += ClientNet_ClientDisconnected;
            _autoClient.ClientMessage += ClientNet_ClientMessage;

            while (true)
            {
                _autoClient.DoEvents();
                Thread.Sleep(1);
            }

            _autoClient.Destroy();
        }

        private static void ClientNet_ClientMessage(string message)
        {
            Logger.Info($"[Client] Server says: {message}");
        }

        private static void ClientNet_ClientDisconnected()
        {
            // Reset the broadcast system to begin looking for server again.
            Logger.Info("[Client] Client disconnected. Resetting broacast client...");

        }

        private static void ClientNet_ClientConnected()
        {
            // Nothing to see here, move along!
        }
    }
}
