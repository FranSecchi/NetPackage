using System;
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
    public class UDPSolution : ITransport, INetEventListener
    {
        private NetManager _peer;
        private Coroutine _pollingCoroutine;
        private Thread _pollingThread;
        private int _port;
        private bool _isServer;
        private bool _isRunning;
        private byte[] _lastPacket;
        
        public event Action<int> OnClientConnected;
        public event Action<int> OnClientDisconnected;
        public event Action OnDataReceived;

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
            
            _isRunning = true;
            _pollingThread = new Thread(PollNetwork);
            _pollingThread.IsBackground = true; // Ensures it stops when Unity closes
            _pollingThread.Start();
        }

        public void Connect(string address, int port)
        {
            if (_isServer) return;
            Debug.Log("Connecting...");
            _peer.Connect(address, port, "Net_Key");
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join();
            }

            _peer.Stop();
        }

        public void Send(byte[] data)
        {
            NetDataWriter writer = new NetDataWriter();      
            writer.Put(data);          
            _peer.SendToAll(writer, DeliveryMethod.Sequenced);
        }

        public byte[] Receive()
        {
            return _lastPacket;
        }

        
        
        
        
        
        
        
        
        public void OnPeerConnected(NetPeer peer)
        {
            if (_isServer)
            {
                Debug.Log("[SERVER] Client connected: "  + peer.Address + ":" + peer.Port);
            }
            else
            {
                Debug.Log($"[CLIENT] Connected to server: "+ peer.Address + ":" + peer.Port);
            }
            OnClientConnected?.Invoke(peer.Id);
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
            OnClientDisconnected?.Invoke(peer.Id);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            throw new NotImplementedException();
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            Debug.Log("Data received from client");
            _lastPacket = reader.GetRemainingBytes();
            OnDataReceived?.Invoke();
            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            if (_isServer)
            {
                // Accept connection requests if the server
                request.AcceptIfKey("Net_Key");
            }
        }
        
        private void PollNetwork()
        {
            while (_isRunning)
            {
                _peer.PollEvents();
                Thread.Sleep(15); // Prevents excessive CPU usage
            }
        }
    }
}