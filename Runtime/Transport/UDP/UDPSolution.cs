using LiteNetLib;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NetManager = LiteNetLib.NetManager;

namespace NetPackage.Runtime.Transport
{
    public class UDPSolution : ITransport, INetEventListener
    {
        private NetManager _peer;
        private int _port;
        private bool _isServer;
        private Coroutine _pollingCoroutine;
        private Thread _pollingThread;
        private bool _isRunning;
        
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

        public void Listen()
        {
            _peer.PollEvents();
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
            if (_isServer)
            {
                Debug.Log("[SERVER] Client connected: "  + peer.Address + ":" + peer.Port);
            }
            else
            {
                Debug.Log($"[CLIENT] Connected to server: "+ peer.Address + ":" + peer.Port);
            }
            OnClientConnected?.Invoke();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Debug.Log($"Client disconnected. Reason: {disconnectInfo.Reason}");
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