using LiteNetLib;
using LiteNetLib.Utils;
using NLog;
using System;
using System.Net;

namespace Network.Common.Server
{
    public class NetServerManager
    {
        public event Action<NetPeer, string> ServerMessage;

        private BroadcastServerManager _serverBroadcaster;
        private EventBasedNetListener listener;
        private IPEndPoint _addressToBroadcast;
        private NetManager _server;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void CreateServer(IPAddress netAddress, int netPort, int broadcastPort)
        {
            // Used to in ACK discovery packet sent back to clients.
            _addressToBroadcast = new IPEndPoint(netAddress, netPort);

            // LiteNetLib UDP server networking library assists us with broadcast packets.
            _serverBroadcaster = new BroadcastServerManager(broadcastPort, _addressToBroadcast);

            listener = new EventBasedNetListener();
            _server = new NetManager(listener)
            {
                //SimulateLatency = true,
                //SimulationMaxLatency = 1500,
                //SimulatePacketLoss = true,
                //SimulationPacketLossChance = 20,
                EnableStatistics = true
            };

            _server.Start(netPort);

            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var message = reader.GetString();
            reader.Recycle();

            // Skip if we get a message from an unknown client.
            if (_server == null)
            {
                return;
            }

            // Swallow ping requests and reply back to clients automatically.
            if (message == "PING!")
            {
                NetDataWriter writer = new();
                var reply = "PONG!";
                writer.Put(reply);
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
                Logger.Info($"[Server] Got {message} from {peer.EndPoint}, replying {reply}");
                return;
            }

            ServerMessage?.Invoke(peer, message);
        }

        public void SendToAll(string data)
        {
            NetDataWriter writer = new();
            writer.Put(data);
            _server.SendToAll(writer, DeliveryMethod.ReliableOrdered);
        }

        public void SendToClient(NetPeer client, string data)
        {
            NetDataWriter writer = new();
            writer.Put(data);
            client.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            Console.WriteLine("We got connection: {0}", peer.EndPoint);
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (_server.ConnectedPeersCount < 10 /* max connections */)
                request.AcceptIfKey("SomeConnectionKey");
            else
                request.Reject();
        }

        public void DoEvents()
        {
            // Process messages from client.
            _server?.PollEvents();
            _serverBroadcaster?.DoEvents();
        }

        public void Destroy()
        {
            _server?.Stop();
            _serverBroadcaster?.Destroy();
        }
    }
}
