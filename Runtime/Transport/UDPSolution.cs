using LiteNetLib;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using NetManager = LiteNetLib.NetManager;

namespace NetPackage.Runtime.Transport
{
    public class UDPSolution : ITransport, INetEventListener
    {
        private NetManager _peer;
        private int _port;
        private bool _isServer;

        public event Action OnClientConnected;
        public void Setup(int port, bool isServer)
        {
            _isServer = isServer;
            _peer = new NetManager(this);
            _port = port;
        }
        public void Start()
        {
            if(_isServer) _peer.Start(_port);
            else _peer.Start();
        }

        public void Connect(string address, int port)
        {
            if (_isServer) return;
            Debug.Log("Connecting...");
            _peer.Connect(address, port, "Net Key");
        }

        public void Disconnect()
        {
            if(_isServer) _peer.DisconnectAll();
            _peer.Stop();
        }

        public void Send(byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] Receive()
        {
            throw new NotImplementedException();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Debug.Log("Client connected");
            OnClientConnected?.Invoke();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log("Client connected");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            throw new NotImplementedException();
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            throw new NotImplementedException();
        }
    }
}