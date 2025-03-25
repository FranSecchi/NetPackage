using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Transport.NetPackage.Runtime.Transport.UDP
{
    public class LANDiscovery 
    {
        private UdpClient _udpClient;
        private const int DiscoveryPort = 9050; // Port for discovery messages
        private const string DiscoveryMessage = "ServerAvailable";
        private Thread _listenThread;
        private bool _isListening;

        public event Action<IPEndPoint> OnServerFound;

        public void StartDiscovery()
        {
            _udpClient = new UdpClient();
            _udpClient.EnableBroadcast = true;
            _isListening = true;
            _listenThread = new Thread(ListenForServers)
            {
                IsBackground = true
            };
            _listenThread.Start();
        }

        private void ListenForServers()
        {
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, DiscoveryPort);
            _udpClient.Client.Bind(groupEP);

            while (_isListening)
            {
                try
                {
                    byte[] bytes = _udpClient.Receive(ref groupEP);
                    string message = System.Text.Encoding.UTF8.GetString(bytes);
                    if (message == DiscoveryMessage)
                    {
                        Debug.Log($"Discovered Server: {groupEP.Address}");
                        OnServerFound?.Invoke(groupEP);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in LAN discovery: {e.Message}");
                }
            }
        }

        public void StopDiscovery()
        {
            _isListening = false;
            _listenThread?.Join();
            _udpClient?.Close();
        }
    }
}
