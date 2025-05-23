using System;
using System.Collections.Generic;
using System.Net;
using NetPackage.Synchronization;
using NetPackage.Serializer;
using NetPackage.Messages;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using NetPackage.Utilities;
using UnityEngine;

namespace NetPackage.Network
{
    public class NetManager : MonoBehaviour
    {
        public NetPrefabRegistry NetPrefabs;
        private static NetManager _manager;
        private static ServerInfo _serverInfo;
        public static ITransport Transport;
        public static int Port = 7777;
        public static List<int> allPlayers;
        private bool _isHost = false;
        private bool _running = false;
        
        [SerializeField] public string serverName = "Net_Server";
        [SerializeField] public int maxPlayers = 10;
        [SerializeField] public bool useLAN = false;
        [SerializeField] public float lanDiscoveryInterval = 0.1f;
        [SerializeField] public float stateUpdateInterval = 0.05f; // 20 updates per second by default
        private float _lastStateUpdate;
        private float _lastLanDiscovery;
        private List<ServerInfo> _discoveredServers = new List<ServerInfo>();
        
        public static NetPrefabRegistry PrefabsList => _manager.NetPrefabs;
        public static bool IsHost => _manager._isHost;
        public static string ServerName => _manager.serverName;
        public static int MaxPlayers => _manager.maxPlayers;
        public static int PlayerCount => allPlayers.Count;
        public static bool Running => _manager._running;
        public static bool Connected => GetConnectionState() == ConnectionState.Connected;
        public static bool Active => _manager != null;
        public static bool UseLan
        {
            get => _manager.useLAN;
            set => _manager.useLAN = value;
        }
        public string address = "localhost";
        public static void SetTransport(ITransport transport)
        {
            Transport = transport;
        }
        private void Awake()
        {
            if (_manager != null)
                Destroy(this);
            else _manager = this;
            Transport ??= new UDPSolution();
            allPlayers = new List<int>();
            if(NetPrefabs != null) NetScene.RegisterPrefabs(NetPrefabs.prefabs);
            _running = false;
            DontDestroyOnLoad(this);
        }

        private readonly Queue<Action> mainThreadActions = new();
        public static void EnqueueMainThread(Action action)
        {
            lock (_manager.mainThreadActions)
            {
                _manager.mainThreadActions.Enqueue(action);
            }
        }
        private void Update()
        {
            lock (mainThreadActions)
            {
                while (mainThreadActions.Count > 0)
                {
                    var action = mainThreadActions.Dequeue();
                    action?.Invoke();
                }
            }
            
            if (_running)
            {
                float currentTime = Time.time;
                if (currentTime - _lastStateUpdate >= stateUpdateInterval)
                {
                    StateManager.SendUpdateStates();
                    _lastStateUpdate = currentTime;
                }
            }
        }

        private void OnDestroy()
        {
            StopNet();
            _manager.mainThreadActions.Clear();
            Messager.ClearHandlers();
            NetScene.CleanUp();
        }

        private void OnApplicationQuit()
        {
            StopNet();
            _manager.mainThreadActions.Clear();
            Messager.ClearHandlers();
            NetScene.CleanUp();
        }

        public static void StartHost(ServerInfo serverInfo = null)
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            if(serverInfo != null)
            {
                if (serverInfo.Address == null)
                    serverInfo.Address = Transport.GetLocalIPAddress();
                _serverInfo = serverInfo;
            }
            else
            {
                _serverInfo = new ServerInfo()
                {
                    CurrentPlayers = 0,
                    MaxPlayers = _manager.maxPlayers,
                    Address = Transport.GetLocalIPAddress(),
                    Port = Port,
                    ServerName = _manager.serverName,
                    GameMode = "Unknown",
                };
            }
            Transport.Setup(Port, true, _serverInfo);
            _manager._isHost = true;
            _manager._running = true;
            NetHost.StartHost();
            if (UseLan)
            {
                Transport.BroadcastServerInfo();
            }
        }
        public static void StartClient()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false);
            _manager._isHost = false;
            _manager._running = true;
            if (!_manager.useLAN)
                NetClient.Connect(_manager.address);
            else
            {
                Transport.StartServerDiscovery(_manager.lanDiscoveryInterval);
                ITransport.OnLanServerUpdate += UpdateLanServers;
                _manager._discoveredServers.Clear();
            }
        }

        public static void ConnectTo(string address)
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false);
            _manager._isHost = false;
            _manager._running = true;
            NetClient.Connect(address);
        }
        public static void StopNet()
        {
            if (!_manager._running) return;
            if (UseLan) StopLan();
            if (IsHost) NetHost.Stop();
            else NetClient.Disconnect();
            allPlayers.Clear();
            NetScene.CleanUp();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
            
            _manager._running = false;
        }

        public static int ConnectionId()
        {
            if (!IsHost) return NetClient.Connection.Id;
            return -1;
        }
        public static void Send(NetMessage netMessage)
        {
            if (!_manager._running) return;
            if(IsHost)
                NetHost.Send(netMessage);
            else NetClient.Send(netMessage);
        }
        
        public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default, int owner = -1)
        {
            if (!_manager._running) return;
            SpawnMessage spm = new SpawnMessage(ConnectionId(), prefab.name, position, rotation, owner);
            spm.requesterId = ConnectionId();
            if (IsHost)
            {
                NetScene.Spawn(spm);
            }
            else
            {
                NetClient.Send(spm);
            }
        }

        public static void Destroy(int netObjectId)
        {
            if (!_manager._running) return;
            var netObj = NetScene.GetNetObject(netObjectId);
            if (netObj != null && (netObj.Owned || IsHost))
            {
                DestroyMessage msg = new DestroyMessage(netObjectId, ConnectionId());
                if (IsHost)
                {
                    NetScene.Destroy(netObjectId);
                    NetHost.Send(msg);
                }
                else
                {
                    NetClient.Send(msg);
                }
            }
        }
        public static void StopLan()
        {
            if (!IsHost)
            {
                ITransport.OnLanServerUpdate -= UpdateLanServers;
                Transport.StopServerDiscovery();
                _manager._discoveredServers.Clear();
            }
            else Transport.StopServerBroadcast();
        }
        public static List<ServerInfo> GetDiscoveredServers()
        {
            return _manager._discoveredServers;
        }

        public static ServerInfo GetServerInfo()
        {
            if(!_manager._running) return null;
            _serverInfo = Transport.GetServerInfo();
            return _serverInfo;
        }
        public static ConnectionInfo GetConnectionInfo(int clientId = 0)
        {
            if(!_manager._running) return null;
            return Transport.GetConnectionInfo(clientId);
        }
        public static ConnectionState? GetConnectionState(int clientId = 0)
        {
            if(!_manager._running) return null;
            return Transport.GetConnectionState(clientId);
        }

        public static void SetServerInfo(ServerInfo serverInfo)
        {
            _serverInfo = serverInfo;
            Transport.SetServerInfo(serverInfo);
            if(IsHost) NetHost.UpdatePlayers(ConnectionId());
        }
        public static void SetServerName(string serverName)
        {
            if (_manager._running && IsHost)
            {
                _serverInfo.ServerName = serverName;
                Transport.SetServerInfo(_serverInfo);
            }
        }

        public static void LoadScene(string sceneName)
        {
            if (!_manager._running) return;
            if(IsHost) NetScene.LoadScene(sceneName);
        }
        public static List<ConnectionInfo> GetClients()
        {
            if(!IsHost) return null;
            List<ConnectionInfo> clients = new List<ConnectionInfo>();
            for (int i = 0; i < allPlayers.Count - 1; i++)
            {
                ConnectionInfo client = Transport.GetConnectionInfo(i);
                if(client != null) clients.Add(client);
            }
            return clients;
        }
        private static void Receive(int id)
        {
            byte[] data = Transport.Receive();
            
            if (data != null && data.Length != 0)
            {
                NetMessage msg = NetSerializer.Deserialize<NetMessage>(data);
                if(msg is not SyncMessage) DebugQueue.AddNetworkMessage(msg);
                Messager.HandleMessage(msg);
            }
        }

        private static void UpdateLanServers(ServerInfo serverInfo)
        {
            var currentServers = Transport.GetDiscoveredServers();
            
            _manager._discoveredServers = new List<ServerInfo>(currentServers);
            DebugQueue.AddMessage($"Updated server list. Current servers: {string.Join(", ", _manager._discoveredServers)}");

        }
    }
}
