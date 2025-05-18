using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace NetPackage.Transport.UDP
{
    public class LANBroadcast
    {
        private UdpClient _udpClient;
        private Thread _broadcastThread;
        private bool _isRunning;
        private const int DiscoveryPort = 8888;
        private const string DiscoveryMessage = "NetPackage_Discovery";
        private ServerInfo _serverInfo;

        public void StartBroadcast()
        {
            if (_isRunning) return;

            try
            {
                _udpClient = new UdpClient();
                _udpClient.EnableBroadcast = true;
                _isRunning = true;
                _broadcastThread = new Thread(BroadcastLoop)
                {
                    IsBackground = true
                };
                _broadcastThread.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start LAN broadcast: {e.Message}");
            }
        }

        public void StopBroadcast()
        {
            _isRunning = false;
            _udpClient?.Close();
            _broadcastThread?.Join();
        }

        public void UpdateServerInfo(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
        }

        public void BroadcastServerInfo(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
        }

        private void BroadcastLoop()
        {
            while (_isRunning)
            {
                try
                {
                    if (_serverInfo != null)
                    {
                        var message = BuildBroadcastMessage();
                        var data = Encoding.ASCII.GetBytes(message);
                        _udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                    }
                    Thread.Sleep(1000); // Broadcast every second
                }
                catch (Exception e)
                {
                    if (_isRunning)
                    {
                        Debug.LogError($"Error in broadcast loop: {e.Message}");
                    }
                }
            }
        }

        private string BuildBroadcastMessage()
        {
            var message = new StringBuilder();
            message.Append(DiscoveryMessage);
            message.Append("|");
            message.Append(_serverInfo.ServerName);
            message.Append("|");
            message.Append(_serverInfo.CurrentPlayers);
            message.Append("|");
            message.Append(_serverInfo.MaxPlayers);
            message.Append("|");
            message.Append(_serverInfo.GameMode);

            // Add custom data
            if (_serverInfo.CustomData != null)
            {
                foreach (var kvp in _serverInfo.CustomData)
                {
                    message.Append("|");
                    message.Append(kvp.Key);
                    message.Append("|");
                    message.Append(kvp.Value);
                }
            }

            return message.ToString();
        }
    }
}
