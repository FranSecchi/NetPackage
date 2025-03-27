using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class UDPSolution : ITransport
    {
        private APeer _aPeer;
        private Thread _pollingThread;
        private bool _isRunning;

        public bool IsHost;

        private LANDiscovery _lanDiscovery;
        private LANBroadcast _lanBroadcaster;
        private List<IPEndPoint> _lanServers;
        public void Setup(int port, bool isServer, bool isBroadcast = false)
        {
            if(_isRunning) Disconnect();
            _aPeer = isServer ? new AHost(port) : new AClient(port);
            IsHost = isServer;

            if (isBroadcast) Discover();
        }

        private void Discover()
        {
            if (IsHost)
            {
                _lanBroadcaster = new LANBroadcast();
                _lanBroadcaster.StartBroadcast();
            }
            else
            {
                _lanServers = new List<IPEndPoint>();
                _lanDiscovery = new LANDiscovery();
                _lanDiscovery.OnServerFound += address =>
                {
                    Debug.Log($"Found server at {address}");
                    if(!_lanServers.Contains(address))
                        _lanServers.Add(address);
                };
                _lanDiscovery.StartDiscovery();
            }
        }

        public void Start()
        {
            _aPeer.Start();
            StartThread();
        }

        public void Connect(string address)
        {
            if(IsHost)
            {
                Debug.Log("[SERVER] Cannot connect to a client as a server.");
                return;
            }

            _aPeer.Connect(address);
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join();
            }
            _aPeer.Stop();
            
            _lanDiscovery?.StopDiscovery();
            _lanBroadcaster?.StopBroadcast();
        }

        public void Kick(int id)
        {
            if (!IsHost) 
            {
                Debug.Log("[Client] Client cannot kick other clients.");
                return;
            }

            _aPeer.Kick(id);
        }
        public void Send(byte[] data)
        {
            _aPeer.Send(data);
        }

        public void SendTo(int id, byte[] data)
        {
            if (!IsHost) 
            {
                Debug.Log("[Client] Client cannot send data to other clients. Use ITransport.Sent instead.");
                return;
            }
            _aPeer.SendTo(id, data);
        }

        public byte[] Receive()
        {
            return _aPeer.Receive();
        }

        public List<IPEndPoint> GetDiscoveredServers()
        {
            return new List<IPEndPoint>(_lanServers);
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
                _aPeer.Poll();
                Thread.Sleep(15); // Prevents excessive CPU usage
            }
        }
    }
}