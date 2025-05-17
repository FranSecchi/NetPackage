using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class AHost : APeer
    {
        private int _connected;
        public AHost(int port) : base(port)
        {
        }

        public override void Start()
        {
            if(UseDebug) Debug.Log($"[SERVER] Listening on port {Port}.");
            Peer.Start(Port);
        }

        public override void Connect(string address)
        {
            if(UseDebug) Debug.Log("[SERVER] Cannot connect to a client as a server.");
        }

        public override void Kick(int id)
        {
            if (Peer.TryGetPeerById(id, out NetPeer peer))
            {
                peer.Disconnect();
                if(UseDebug) Debug.Log($"[SERVER] Client {id} kicked.");
            }
        }

        public override void Send(byte[] data)
        {
            List<NetPeer> peers = new List<NetPeer>(Peer.ConnectedPeerList);
            foreach (var peer in peers)
            {
                if(peer.ConnectionState == LiteNetLib.ConnectionState.Connected)
                    peer.Send(data, DeliveryMethod.Sequenced);
            }
            if(UseDebug) Debug.Log("[SERVER] Sent message to all clients");
        }

        public override void OnConnectionRequest(ConnectionRequest request)
        {
            if(UseDebug) Debug.Log($"[SERVER] Requested connection from {request.RemoteEndPoint}.");
            if(_connected < MaxPlayers) request.AcceptIfKey("Net_Key");
            else if(UseDebug) Debug.Log($"[SERVER] Requested connection denied from {request.RemoteEndPoint}. Max players: {MaxPlayers}");
        }


        public override void OnPeerConnected(NetPeer peer)
        {
            if(UseDebug) Debug.Log("[SERVER] Client connected: " + peer.Address + "|" + peer.Port + ":" + peer.Id);
            ITransport.TriggerOnClientConnected(peer.Id);
            UpdateConnectionInfo(peer.Id, ConnectionState.Connected, peer.Ping);
        }
        
        public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if(UseDebug) Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
            ITransport.TriggerOnClientDisconnected(peer.Id);
            UpdateConnectionInfo(peer.Id, ConnectionState.Disconnected, peer.Ping);
        }
    }
}
