using LiteNetLib;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class AClient : APeer
    {
        public AClient(int port) : base(port)
        {
        }

        public override void Connect(string address)
        {
            Debug.Log($"Connecting to: {address}:{Port}");
            Peer.Connect(address, Port, "Net_Key");
        }

        public override void Kick(int id)
        {
            Debug.Log("[CLIENT] Kicked from host");
            Peer.DisconnectPeer(Peer.FirstPeer);
        }

        public override void Start()
        {
            Peer.Start();
        }
        
        
        
        public override void Send(byte[] data)
        {
            Peer.FirstPeer.Send(data, DeliveryMethod.Sequenced);
            Debug.Log("[CLIENT] Sent message to host");
        }
        

        public override void OnPeerConnected(NetPeer peer)
        {
            Debug.Log($"[CLIENT] Connected to server: "+ peer.Address + ":" + peer.Port);
            ITransport.TriggerOnClientConnected(peer.Id);
        }
        
        public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        }
    }
}
