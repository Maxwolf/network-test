using LiteNetLib;
using LiteNetLib.Utils;
using NLog;
using System;
using System.Net;

namespace Network.Common
{
    /// <summary>
    /// LiteNetLib UDP client networking library assists us with broadcast packets.
    /// </summary>
    public class BroadcastClientManager
    {
        private readonly BroadcastClient _clientListener;
        private readonly NetManager _client;
        private bool _foundServer = false;
        private int retryTicks;
        private const int _retryTotalTicks = 100;
        private readonly int _broadcastPort;
        private IPEndPoint _foundServerAddress;
        public bool Connected;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Once client gets a reply from a server it stores the remote end point address here.
        /// </summary>
        public IPEndPoint FoundServerAddress { get => _foundServerAddress; }

        /// <summary>
        /// Determines if the broadcast client process has located a valid server.
        /// </summary>
        public bool FoundServer { get => _foundServer; }

        public event Action<IPEndPoint> BroadcastFoundServer;

        /// <summary>
        /// Total packets sent from client to server.
        /// </summary>
        public ulong PacketsSent { get; private set; }

        /// <summary>
        /// Total packets received from server.
        /// </summary>
        public ulong PacketsReceived { get; private set; }

        public BroadcastClientManager(int broadcastPort)
        {
            Logger.Info("[Client] Broadcast client starting...");

            //Client
            _broadcastPort = broadcastPort;
            _clientListener = new BroadcastClient();
            _client = new NetManager(_clientListener)
            {
                UnconnectedMessagesEnabled = true,
                SimulateLatency = true,
                SimulationMaxLatency = 1500,
                IPv6Enabled = IPv6Mode.Disabled
            };

            _clientListener.FoundServerEvent += FoundServerEvent;
            _clientListener.Client = _client;
            if (!_client.Start())
            {
                Logger.Info("[Client] Client start failed");
                return;
            }
        }

        /// <summary>
        /// KA-BOOOOOOOOM!
        /// </summary>
        public void Destroy()
        {
            _client.Stop();
        }

        /// <summary>
        /// Broadcast packet read by server and they replied! We found them!
        /// </summary>
        private void FoundServerEvent(IPEndPoint e)
        {
            // Skip if already found server.
            if (_foundServer)
            {
                return;
            }

            Logger.Info("[Client] Got response from server, stopping broadcast now.");
            PacketsReceived++;
            _foundServer = true;
            retryTicks = 0;
            _client.Stop();

            // For use with another networking system to know where to connect will have its own port.
            _foundServerAddress = e;
            BroadcastFoundServer?.Invoke(_foundServerAddress);
        }

        /// <summary>
        /// Pump messages for client broadcast system.
        /// </summary>
        public void DoEvents()
        {
            _client.PollEvents();

            // Keep sending every 5 seconds until we find our bae.
            if (!_foundServer)
            {
                // Increment timer to total wait time before sending another packet.
                if (retryTicks <= _retryTotalTicks)
                {
                    retryTicks++;
                    return;
                }

                Logger.Info("[Client] Sent broadcast packet looking for server!");

                // Reset retry timer to zero.
                retryTicks = 0;

                //Send broadcast
                NetDataWriter writer = new();

                writer.Put("CLIENT_DISCOVERY");
                _client.SendBroadcast(writer, _broadcastPort);
                PacketsSent++;
                writer.Reset();
            }
        }

        /// <summary>
        /// Begins looking for the server again, used after disconnection events.
        /// </summary>
        public void ResetSeverFoundState()
        {
            _foundServer = false;
            retryTicks = 0;
            _foundServerAddress = null;

            if (!_client.Start())
            {
                Logger.Info("[Client] Client start failed");
            }
        }
    }
}
