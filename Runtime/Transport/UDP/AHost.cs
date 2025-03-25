using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class AHost : APeer
    {
        public AHost(int port) : base(port)
        {
        }

        public override void Start()
        {
            Debug.Log($"[SERVER] Listening on port {Port}.");
            Peer.Start(Port);
        }

        public override void Connect(string address)
        {
            Debug.Log("[SERVER] Cannot connect to a client as a server.");
        }

        public override void Kick(int id)
        {
            if (Peer.TryGetPeerById(id, out NetPeer peer))
            {
                peer.Disconnect();
                Debug.Log($"[SERVER] Client {id} kicked.");
            }
        }

        public override void Send(byte[] data)
        {
            foreach (var peer in Peer.ConnectedPeerList)
            {
                peer.Send(data, DeliveryMethod.Sequenced);
            }
            Debug.Log("[SERVER] Sent message to all clients");
        }
        

        public override void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("[SERVER] Client connected: " + peer.Address + "|" + peer.Port + ":" + peer.Id);
            ITransport.TriggerOnClientConnected(peer.Id);
        }
        
        public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
            ITransport.TriggerOnClientDisconnected(peer.Id);
        }
    }
}
