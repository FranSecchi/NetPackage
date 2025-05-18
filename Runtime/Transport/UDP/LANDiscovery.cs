using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace NetPackage.Transport.UDP
{
    public class LANDiscovery
    {
        private UdpClient _udpClient;
        private Thread _discoveryThread;
        private bool _isRunning;
        private const int DiscoveryPort = 8888;
        private const string DiscoveryMessage = "NetPackage_Discovery";
        private const double ServerTimeoutSeconds = 5.0;

        private Dictionary<IPEndPoint, (ServerInfo Info, DateTime LastSeenTime)> _knownServers = new Dictionary<IPEndPoint, (ServerInfo, DateTime)>();

        public event Action<ServerInfo> OnServerFound;
        public event Action<ServerInfo> OnServerLost;

        public void StartDiscovery(int port = DiscoveryPort)
        {
            if (_isRunning) return;

            try
            {
                _udpClient = new UdpClient();
                _udpClient.EnableBroadcast = true;
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                _isRunning = true;
                _discoveryThread = new Thread(DiscoveryLoop)
                {
                    IsBackground = true
                };
                _discoveryThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start LAN discovery: {e.Message}");
            }
        }

        public void StopDiscovery()
        {
            _isRunning = false;
            _udpClient?.Close();
            _discoveryThread?.Join();
            _knownServers.Clear();
        }

        private void DiscoveryLoop()
        {
            DateTime lastTimeoutCheck = DateTime.UtcNow;
            while (_isRunning)
            {
                try
                {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = _udpClient.Receive(ref remoteEndPoint);
                    var message = Encoding.ASCII.GetString(data);

                    if (message.StartsWith(DiscoveryMessage))
                    {
                        var serverInfo = ParseServerInfo(message, remoteEndPoint);
                        UpdateServerInfo(serverInfo);
                    }

                    // Check for timed out servers every 100ms
                    if ((DateTime.UtcNow - lastTimeoutCheck).TotalMilliseconds >= 100)
                    {
                        CheckForTimedOutServers();
                        lastTimeoutCheck = DateTime.UtcNow;
                    }
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"Error in discovery loop: {e.Message}");
                    }
                }
            }
        }

        private void UpdateServerInfo(ServerInfo serverInfo)
        {
            DateTime currentTime = DateTime.UtcNow;
            bool isNewServer = !_knownServers.ContainsKey(serverInfo.EndPoint);

            _knownServers[serverInfo.EndPoint] = (serverInfo, currentTime);

            OnServerFound?.Invoke(serverInfo);
        }

        private void CheckForTimedOutServers()
        {
            DateTime currentTime = DateTime.UtcNow;
            var timedOutServers = new List<IPEndPoint>();

            foreach (var kvp in _knownServers)
            {
                var timeSinceLastSeen = (currentTime - kvp.Value.LastSeenTime).TotalSeconds;
                
                if (timeSinceLastSeen > ServerTimeoutSeconds)
                {
                    timedOutServers.Add(kvp.Key);
                }
            }

            foreach (var endPoint in timedOutServers)
            {
                var serverInfo = _knownServers[endPoint].Info;
                _knownServers.Remove(endPoint);
                OnServerLost?.Invoke(serverInfo);
            }
        }

        private ServerInfo ParseServerInfo(string message, IPEndPoint endPoint)
        {
            var parts = message.Split('|');
            var serverInfo = new ServerInfo
            {
                EndPoint = endPoint,
                ServerName = parts.Length > 1 ? parts[1] : "Unknown Server",
                CurrentPlayers = parts.Length > 2 ? int.Parse(parts[2]) : 0,
                MaxPlayers = parts.Length > 3 ? int.Parse(parts[3]) : 0,
                GameMode = parts.Length > 4 ? parts[4] : "Unknown",
                Ping = 0,
                CustomData = new System.Collections.Generic.Dictionary<string, string>()
            };

            // Parse custom data if present
            if (parts.Length > 5)
            {
                for (int i = 5; i < parts.Length; i += 2)
                {
                    if (i + 1 < parts.Length)
                    {
                        serverInfo.CustomData[parts[i]] = parts[i + 1];
                    }
                }
            }

            return serverInfo;
        }
    }
}
