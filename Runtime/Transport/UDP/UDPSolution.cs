using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using NetManager = LiteNetLib.NetManager;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class UDPSolution : ITransport, INetEventListener
    {
        private NetManager Peer;
        private byte[] LastPacket;
        
        private NetPeer _server;
        private Thread _pollingThread;
        private bool _isRunning;
        private bool _isServer;
        private int _port;
        private Dictionary<int, NetPeer> _connectedClients;

        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action OnDataReceived;

        
        public void Setup(int port, bool isServer)
        {
            Peer = new NetManager(this);
            _connectedClients = new Dictionary<int, NetPeer>();
            _port = port;
            _isServer = isServer;
        }

        public void Start()
        {
            if(_isServer)
                Peer.Start(_port);
            else Peer.Start();
            StartThread();
        }

        public void Connect(string address)
        {
            if(_isServer)
            {
                Debug.Log("[SERVER] Cannot connect to a client as a server.");
                return;
            }
            Debug.Log($"Connecting to: {address}:{_port}");
            Peer.Connect(address, _port, "Net_Key");
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join();
            }

            Peer.Stop();
        }

        public void Kick(int id)
        {
            if (!_isServer) return;
            if (_connectedClients.TryGetValue(id, out NetPeer peer))
            {
                peer.Disconnect();
                Debug.Log($"[SERVER] Client {id} kicked.");
            }
        }
        public void Send(byte[] data)
        {
            if (_isServer)
            {
                foreach (var peer in _connectedClients.Values)
                {
                    peer.Send(data, DeliveryMethod.Sequenced);
                }
                Debug.Log("[SERVER] Sent message to all clients");
            }
            else
            {
                _server.Send(data, DeliveryMethod.Sequenced);
                Debug.Log("[CLIENT] Sent message to host");
            }
        }

        public void SendTo(int id, byte[] data)
        {
            if (!_connectedClients.TryGetValue(id, out NetPeer peer)) return;
            peer.Send(data, DeliveryMethod.Sequenced);
            Debug.Log($"[SERVER] Sent message to client {id}");
        }

        public byte[] Receive()
        {
            return LastPacket;
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("Net_Key");
        }


        public void OnPeerConnected(NetPeer peer)
        {
            if(_isServer)
            {
                Debug.Log("[SERVER] Client connected: " + peer.Address + ":" + peer.Port);
                _connectedClients[peer.Id] = peer;
            }
            else
            {
                _server = peer;
                Debug.Log($"[CLIENT] Connected to server: "+ peer.Address + ":" + peer.Port);
            }
            OnClientConnected?.Invoke(peer.Id);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (_isServer)
            {
                Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
                _connectedClients.Remove(peer.Id);
            }
            else
            {
                Debug.Log($"Disconnected from server. Reason: {disconnectInfo.Reason}");
            }
            OnClientDisconnected?.Invoke(peer.Id);
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Debug.Log("Data received");
            LastPacket = reader.GetRemainingBytes();
            OnDataReceived?.Invoke();
            reader.Recycle();
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
        
        private void StartThread()
        {
            _pollingThread = new Thread(PollNetwork)
            {
                IsBackground = true // Ensures it stops when Unity closes
            };
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