using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;
using static NetPackage.Transport.ITransport;

namespace NetPackage.Transport.UDP
{
    public class UDPSolution : ITransport
    {
        private APeer _aPeer;
        private Thread _pollingThread;
        private bool _isRunning;
        
        public bool IsHost;
        private bool _useDebug;
        private LANDiscovery _lanDiscovery;
        private LANBroadcast _lanBroadcaster;
        private List<ServerInfo> _lanServers;
        private ServerInfo _serverInfo;
        private int _bandwidthLimit;

        public UDPSolution()
        {
            _lanServers = new List<ServerInfo>();
            _serverInfo = new ServerInfo
            {
                CustomData = new Dictionary<string, string>()
            };
        }

        public void Setup(int port, bool isServer, int maxPlayers = 10, bool useDebug = false)
        {
            if(_isRunning) Disconnect();
            _aPeer = isServer ? new AHost(port) : new AClient(port);
            _aPeer.MaxPlayers = maxPlayers;
            _aPeer.UseDebug = useDebug;
            IsHost = isServer;
            _useDebug = useDebug;
            _serverInfo = new ServerInfo{CurrentPlayers = 0, MaxPlayers = maxPlayers, ServerName = "New_Server"};
        }

        public void Setup(int port, ServerInfo serverInfo, bool useDebug = false)
        {
            if(_isRunning) Disconnect();
            _aPeer = new AHost(port);
            _aPeer.MaxPlayers = serverInfo.MaxPlayers;
            _aPeer.UseDebug = useDebug;
            IsHost = true;
            _useDebug = useDebug;
            _serverInfo = serverInfo;
        }

        public void Start()
        {
            _aPeer.Start();
            StartThread();
        }

        public void Stop()
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

        public void Connect(string address)
        {
            if(IsHost)
            {
                if(_useDebug) Debug.Log("[SERVER] Cannot connect to a client as a server.");
                return;
            }

            _aPeer.Connect(address);
        }

        public void Disconnect()
        {
            _aPeer.Disconnect();
        }

        public void Kick(int id)
        {
            if (!IsHost) 
            {
                if(_useDebug) Debug.Log("[Client] Client cannot kick other clients.");
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
                if(_useDebug) Debug.Log("[Client] Client cannot send data to other clients. Use ITransport.Send instead.");
                return;
            }
            _aPeer.SendTo(id, data);
        }

        public byte[] Receive()
        {
            return _aPeer.Receive();
        }

        public List<ServerInfo> GetDiscoveredServers()
        {
            return new List<ServerInfo>(_lanServers);
        }

        public ConnectionInfo GetConnectionInfo(int clientId)
        {
            return _aPeer.ConnectionInfo.TryGetValue(clientId, out var info) ? info : null;
        }

        public ConnectionState GetConnectionState(int clientId)
        {
            return _aPeer.ConnectionInfo.TryGetValue(clientId, out var info) ? info.State : ConnectionState.Disconnected;
        }

        public void SetServerInfo(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
            if (IsHost)
            {
                _lanBroadcaster?.UpdateServerInfo(serverInfo);
            }
        }
        public ServerInfo GetServerInfo()
        {
            return _serverInfo;
        }
        public void UpdateServerInfo(Dictionary<string, string> customData)
        {
            foreach (var kvp in customData)
            {
                _serverInfo.CustomData[kvp.Key] = kvp.Value;
            }
            if (IsHost)
            {
                _lanBroadcaster?.UpdateServerInfo(_serverInfo);
            }
        }
        
        public void SetBandwidthLimit(int bytesPerSecond)
        {
            _bandwidthLimit = bytesPerSecond;
            _aPeer?.SetBandwidthLimit(bytesPerSecond);
        }

        public void StartServerDiscovery(int discoveryPort = -1)
        {
            if (!IsHost)
            {
                _lanServers = new List<ServerInfo>();
                _lanDiscovery = new LANDiscovery();
                _lanDiscovery.OnServerFound += serverInfo =>
                {
                    if(_lanServers.Contains(serverInfo))
                    {
                        SetServerInfo(serverInfo);
                        if(_useDebug) Debug.Log($"Ping server at {serverInfo.EndPoint}");
                    }
                    else
                    {
                        _lanServers.Add(serverInfo);
                        if(_useDebug) Debug.Log($"Found new server at {serverInfo.EndPoint}");
                    }
                    TriggerOnLanServersUpdate(serverInfo);
                };
                _lanDiscovery.OnServerLost += serverInfo =>
                {
                    if(_useDebug) Debug.Log($"Lost server at {serverInfo.EndPoint}");
                    _lanServers.Remove(serverInfo);
                    TriggerOnLanServersUpdate(serverInfo);
                };
                if(discoveryPort == -1) _lanDiscovery.StartDiscovery();
                else _lanDiscovery.StartDiscovery(discoveryPort);
            }
        }
        public void BroadcastServerInfo()
        {
            if (IsHost)
            {
                _lanBroadcaster = new LANBroadcast();
                _lanBroadcaster.StartBroadcast();
                _lanBroadcaster.BroadcastServerInfo(_serverInfo);
            }
        }
        public void StopServerDiscovery()
        {
            _lanDiscovery?.StopDiscovery();
        }
        public void StopServerBroadcast()
        {
            _lanBroadcaster?.StopBroadcast();
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