using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using NetPackage.Runtime.Transport;
using UnityEngine;
using NetManager = LiteNetLib.NetManager;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public abstract class UDPSolution : ITransport, INetEventListener
    {
        protected NetManager Peer;
        protected byte[] LastPacket;
        
        private Thread _pollingThread;
        private bool _isRunning;


        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action OnDataReceived;

        protected void InvokeClientConnected(int clientId) => OnClientConnected?.Invoke(clientId);
        protected void InvokeClientDisconnected(int clientId) => OnClientDisconnected?.Invoke(clientId);
        protected void InvokeDataReceived() => OnDataReceived?.Invoke();
        
        public virtual void Setup(int port)
        {
            Peer = new NetManager(this);
        }
        public abstract void Start();
        public abstract void Connect(string address, int port);
        public abstract void Send(byte[] data);
        public abstract void SendTo(int id, byte[] data);
        public abstract byte[] Receive();
        public virtual void Disconnect()
        {
            _isRunning = false;

            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join();
            }

            Peer.Stop();
        }
        
        public abstract void OnPeerConnected(NetPeer peer);
        public abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);
        public abstract void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod);
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("Net_Key");
        }
        
        
        
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
        }


        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
        
        protected void StartThread()
        {
            _pollingThread = new Thread(PollNetwork);
            _pollingThread.IsBackground = true; // Ensures it stops when Unity closes
            _pollingThread.Start();
        }
        private void PollNetwork()
        {
            _isRunning = true;
            while (_isRunning)
            {
                Peer.PollEvents();
                Thread.Sleep(15); // Prevents excessive CPU usage
            }
        }
    }
}