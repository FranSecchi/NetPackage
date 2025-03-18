using LiteNetLib;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class UDPClient : UDPSolution
    {
        // private NetPeer _server;
        // public override void Start()
        // {
        //     Peer.Start();
        //     StartThread();
        // }
        //
        // public override void Connect(string address, int port)
        // {
        //     Debug.Log("Connecting...");
        //     Peer.Connect(address, port, "Net_Key");
        // }
        //
        //
        // public override void Send(byte[] data)
        // {
        //     _server.Send(data, DeliveryMethod.Sequenced);
        //     Debug.Log("[CLIENT] Sent message to host");
        // }
        //
        // public override void SendTo(int id, byte[] data)
        // {
        //     throw new System.NotImplementedException();
        // }
        //
        // public override byte[] Receive()
        // {
        //     return LastPacket;
        // }
        //
        //
        // public override void OnPeerConnected(NetPeer peer)
        // {
        //     _server = peer;
        //     Debug.Log($"[CLIENT] Connected to server: "+ peer.Address + ":" + peer.Port);
        // }
        //
        // public override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        // {
        //     Debug.Log($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        // }
        //
        // public override void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        // {
        //     Debug.Log("Data received from server");
        //     LastPacket = reader.GetRemainingBytes();
        //     InvokeDataReceived();
        //     reader.Recycle();
        // }
    }
}
