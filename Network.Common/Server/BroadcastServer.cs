using LiteNetLib;
using LiteNetLib.Utils;
using NLog;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Network.Common
{
    public class BroadcastServer : INetEventListener
    {
        public NetManager Server { get; set; }

        private readonly IPEndPoint _serverAddress;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public BroadcastServer(IPEndPoint serverToBroadcast)
        {
            _serverAddress = serverToBroadcast;
            Logger.Info($"[Server] IP Address: {_serverAddress}");
        }

        /// <summary>
        /// Total packets sent from client to server.
        /// </summary>
        public ulong PacketsSent { get; private set; }

        /// <summary>
        /// Total packets received from server.
        /// </summary>
        public ulong PacketsReceived { get; private set; }

        public void OnPeerConnected(NetPeer peer)
        {
            throw new NotImplementedException();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectInfo.Reason);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
        {
            Logger.Error("[Server] error: " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Logger.Info("[Server] ReceiveUnconnected {0}. From: {1}. Data: {2}", messageType, remoteEndPoint, reader.GetString(100));
            PacketsReceived++;

            // TODO Create protobuf broadcast auth packet and serialize it.
            NetDataWriter writer = new();
            writer.Put($"ACK:{GetMachineIpAddress()}:{_serverAddress.Port}");
            Server.SendUnconnectedMessage(writer, remoteEndPoint);
            PacketsSent++;
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Nothing to see here, move along!
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }

        private static IPAddress GetMachineIpAddress()
        {
            var computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            var nics = NetworkInterface.GetAllNetworkInterfaces();

            if (nics == null || nics.Length < 1)
            {
                throw new ApplicationException("No network interfaces found.");
            }

            foreach (var adapter in nics)
            {
                if (adapter.OperationalStatus != OperationalStatus.Up ||
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                {
                    continue;
                }

                var properties = adapter.GetIPProperties();

                var unicastAddress = properties.UnicastAddresses.FirstOrDefault(x => x.PrefixLength == 24);

                if (unicastAddress == null)
                {
                    throw new ApplicationException("No IPv4 address found...");
                }

                return unicastAddress.Address;
            }

            throw new ApplicationException("No suitable network interface found.");
        }
    }
}
