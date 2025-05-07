using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using UnityEngine;
using static Transport.NetPackage.Runtime.Transport.ITransport;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public abstract class APeer : INetEventListener
    {
        protected readonly NetManager Peer;
        protected readonly int Port;
        private readonly ConcurrentQueue<byte[]> _packetQueue = new ConcurrentQueue<byte[]>();

        protected APeer(int port)
        {
            Peer = new NetManager(this);
            Port = port;
        }
        
        public abstract void Start();
        public abstract void Connect(string address);
        public abstract void Kick(int id);
        public abstract void OnPeerConnected(NetPeer peer);
        public abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
        public abstract void Send(byte[] data);
        public void Disconnect()
        {
            Debug.Log($"All Peers disconnected");
            Peer.DisconnectAll();
        }
        public void SendTo(int id, byte[] data)
        {
            if (!Peer.TryGetPeerById(id, out NetPeer peer)) return;
            peer.Send(data, DeliveryMethod.Sequenced);
            Debug.Log($"[SERVER] Sent message to client {id}");
        }
        public byte[] Receive()
        {
            if (_packetQueue.TryDequeue(out byte[] packet))
            {
                return packet;
            }
            return null;
        }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            Debug.Log($"[SERVER] Requested connection from {request.RemoteEndPoint}.");
            request.AcceptIfKey("Net_Key");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Debug.Log("Data received from peer " + peer.Address + "|" + peer.Port + ":" + peer.Id);
            _packetQueue.Enqueue(reader.GetRemainingBytes());
            TriggerOnDataReceived(peer.Id);
            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }
        
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            
        }
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            
        }

        public void Poll()
        {
            Peer.PollEvents();
        }

        public void Stop()
        {
            Peer.DisconnectAll();
            Peer.Stop();
        }
    }
}
