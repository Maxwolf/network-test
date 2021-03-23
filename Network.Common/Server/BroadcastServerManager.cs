using LiteNetLib;
using NLog;
using System.Net;

namespace Network.Common.Server
{
    /// <summary>
    /// LiteNetLib UDP server networking library assists us with broadcast packets.
    /// </summary>
    public class BroadcastServerManager
    {
        private readonly BroadcastServer _serverListener;
        private readonly NetManager _server;

        /// <summary>
        /// Total packets sent from client to server.
        /// </summary>
        public ulong PacketsSent { get => _serverListener.PacketsSent; }

        /// <summary>
        /// Total packets received from server.
        /// </summary>
        public ulong PacketsReceived { get => _serverListener.PacketsReceived; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BroadcastServerManager(int broadcastPort, IPEndPoint serverToBroadcast)
        {
            Logger.Info("[Server] Broadcast server starting...");

            //Server
            _serverListener = new BroadcastServer(serverToBroadcast);

            _server = new NetManager(_serverListener)
            {
                BroadcastReceiveEnabled = true,
                IPv6Enabled = IPv6Mode.Disabled
            };

            if (!_server.Start(broadcastPort))
            {
                Logger.Info("[Server] Server start failed");
                return;
            }

            // This has to be added after start.
            _serverListener.Server = _server;
        }

        /// <summary>
        /// Blow it all up!
        /// </summary>
        public void Destroy()
        {
            _server.Stop();
        }

        /// <summary>
        /// Pump messages for server broadcast system.
        /// </summary>
        public void DoEvents()
        {
            _server.PollEvents();
        }
    }
}
