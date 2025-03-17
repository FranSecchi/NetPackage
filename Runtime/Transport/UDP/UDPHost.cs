using System.Collections.Generic;
using LiteNetLib;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class UDPHost : UDPSolution
    {
        private int _port;
        private Dictionary<int, NetPeer> _connectedClients;
        public override void Setup(int port)
        {
            base.Setup(port);
            
            _connectedClients = new Dictionary<int, NetPeer>();
            _port = port;
        }

        public override void Start()
        {
            Peer.Start(_port);
            StartThread();
        }

        public override void Connect(string address, int port)
        {
            Debug.Log("[SERVER] Cannot connect to a client as a server.");
        }


        public override void Send(byte[] data)
        {
            foreach (var peer in _connectedClients.Values)
            {
                peer.Send(data, DeliveryMethod.Sequenced);
            }
            Debug.Log("[SERVER] Sent message to all clients");
        }

        public override void SendTo(int id, byte[] data)
        {
            if (!_connectedClients.TryGetValue(id, out NetPeer peer)) return;
            peer.Send(data, DeliveryMethod.Sequenced);
            Debug.Log($"[SERVER] Sent message to client {id}");
        }

        public override byte[] Receive()
        {
            return LastPacket;
        }


        public override void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[SERVER] Client connected: "  + peer.Address + ":" + peer.Port);
            _connectedClients[peer.Id] = peer;
            InvokeClientConnected(peer.Id);
        }

        public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
            _connectedClients.Remove(peer.Id);
            InvokeClientDisconnected(peer.Id);
        }

        public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Debug.Log("Data received from client");
            LastPacket = reader.GetRemainingBytes();
            InvokeDataReceived();
            reader.Recycle();
        }
    }
}
