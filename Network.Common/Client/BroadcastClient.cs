using LiteNetLib;
using NLog;
using System;
using System.Net;
using System.Net.Sockets;

namespace Network.Common
{
    public class BroadcastClient : INetEventListener
    {
        public NetManager Client { get; set; }

        public event Action<IPEndPoint> FoundServerEvent;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public void OnPeerConnected(NetPeer peer)
        {
            Logger.Info("[Client {0}] connected to: {1}:{2}", Client.LocalPort, peer.EndPoint.Address, peer.EndPoint.Port);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info("[Client] disconnected: " + disconnectInfo.Reason);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError error)
        {
            Logger.Error("[Client] error! " + error);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            var text = reader.GetString(100);
            Logger.Info("[Client] ReceiveUnconnected {0}. From: {1}. Data: {2}", messageType, remoteEndPoint, text);

            // TODO Parse protobuf packet type, ignore if cast fails.
            if (messageType == UnconnectedMessageType.BasicMessage && text.Substring(0, 3) == "ACK")
            {
                var discoveryData = text.Split(':');
                var parsedAck = new IPEndPoint(IPAddress.Parse(discoveryData[1]), int.Parse(discoveryData[2]));

                Client.DisconnectAll();

                // Pass eventargs object with address and port of server.
                FoundServerEvent?.Invoke(parsedAck);

                return;
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            // Nothing to see here, move along!
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}
