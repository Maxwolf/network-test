using LiteNetLib;
using LiteNetLib.Utils;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;

namespace Network.Common.Client
{
    public class NetClientManager
    {
        private readonly BroadcastClientManager _clientBroadcaster;
        private int _heartTick;
        private EventBasedNetListener listener;
        private NetManager _client;
        private NetPeer _peer;
        private const int _heartTickTotal = 100;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public bool Connected { get => _client != null && _client.IsRunning; }

        public event Action<string> ClientMessage;
        public event Action ClientConnected;
        public event Action ClientDisconnected;

        public NetClientManager(int broadcastPort)
        {
            // Used for broadcast packets specifcally and nothing else.
            _clientBroadcaster = new BroadcastClientManager(broadcastPort);
            _clientBroadcaster.BroadcastFoundServer += BroadcastFoundServer;
        }

        public void DoEvents()
        {
            // Always tick broadcaster, it only does work when no found server.
            _clientBroadcaster.DoEvents();

            // Skip tick if no net client created to work with yet.
            if (_client == null)
            {
                return;
            }

            // Skip if not connected to net client.
            if (!_client.IsRunning)
            {
                return;
            }

            // Heartbeat
            if (_heartTick >= _heartTickTotal)
            {
                SendToServer("PING!");
                Logger.Info("[Client] Sending PING! to server!");

                _heartTick = 0;
                return;
            }

            // Increment heartbeat tick.
            _heartTick++;
        }

        public void SendToServer(string message)
        {
            if (_client == null)
            {
                Logger.Error("[Client] Attempted to send message with null client!");
                return;
            }

            _client.PollEvents();

            if (string.IsNullOrEmpty(message))
            {
                Logger.Warn("[Client] Attempted to send empty message to server. Skipped!");
                return;
            }

            // Attempt to send message to server, errors mean communication problem.
            NetDataWriter writer = new();
            writer.Put(message);
            _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void BroadcastFoundServer(IPEndPoint foundServer)
        {
            Logger.Info($"[Client] Connecting to server at {_clientBroadcaster.FoundServerAddress}...");

            listener = new EventBasedNetListener();
            _client = new NetManager(listener)
            {
                //SimulateLatency = true,
                //SimulationMaxLatency = 1500,
                //SimulatePacketLoss = true,
                //SimulationPacketLossChance = 20,
                EnableStatistics = true
            };

            _client.Start();

            _peer = _client.Connect(foundServer.Address.ToString(), foundServer.Port, "SomeConnectionKey");
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.NetworkErrorEvent += Listener_NetworkErrorEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info($"[Client] Disconnected from server! {disconnectInfo.AdditionalData}");
            ClientDisconnected?.Invoke();

            _client = null;

            _heartTick = 0;

            // Begin looking for server again!
            _clientBroadcaster.ResetSeverFoundState();
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Logger.Info($"[Client] Connected to server!");
            ClientConnected?.Invoke();
        }

        private void Listener_NetworkErrorEvent(IPEndPoint endPoint, SocketError socketError)
        {
            // Error sending a message triggers a disconnect state.
            Logger.Error($"Error sending message: {socketError}");
            Destroy();
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var message = reader.GetString();
            reader.Recycle();

            // Swallow PONG! heartbeat messages from server.
            if (message == "PONG!")
            {
                Logger.Info($"[Client] Got {message}");
                return;
            }

            // Bubbles up non-heartbeat server messages for processing.
            Logger.Info($"[Client] Server says: {message}");
            ClientMessage?.Invoke(message);
        }

        /// <summary>
        /// Clears the network client and server address
        /// </summary>
        public void Destroy()
        {
            _client.Stop();
        }
    }
}
