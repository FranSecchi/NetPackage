using System.Threading;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class UDPSolution : ITransport
    {
        private UDPPeer _udpPeer;
        private Thread _pollingThread;
        private bool _isRunning;
        
        public bool IsHost;

        
        public void Setup(int port, bool isServer)
        {
            _udpPeer = isServer ? new UDPHost(port) : new UDPClient(port);
            IsHost = isServer;
        }

        public void Start()
        {
            _udpPeer.Start();
            StartThread();
        }

        public void Connect(string address)
        {
            if(IsHost)
            {
                Debug.Log("[SERVER] Cannot connect to a client as a server.");
                return;
            }

            _udpPeer.Connect(address);
        }

        public void Disconnect()
        {
            _isRunning = false;

            if (_pollingThread != null && _pollingThread.IsAlive)
            {
                _pollingThread.Join();
            }

            _udpPeer.Stop();
        }

        public void Kick(int id)
        {
            if (!IsHost) 
            {
                Debug.Log("[Client] Client cannot kick other clients.");
                return;
            }

            _udpPeer.Kick(id);
        }
        public void Send(byte[] data)
        {
            _udpPeer.Send(data);
        }

        public void SendTo(int id, byte[] data)
        {
            if (!IsHost) 
            {
                Debug.Log("[Client] Client cannot send data to other clients. Use ITransport.Sent instead.");
                return;
            }
            _udpPeer.SendTo(id, data);
        }

        public byte[] Receive()
        {
            return _udpPeer.Receive();
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
                _udpPeer.Poll();
                Thread.Sleep(15); // Prevents excessive CPU usage
            }
        }
    }
}