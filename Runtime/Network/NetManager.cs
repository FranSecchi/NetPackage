using System;
using System.Collections.Generic;
using System.Net;
using NetPackage.Synchronization;
using NetPackage.Serializer;
using NetPackage.Messages;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using UnityEngine;

namespace NetPackage.Network
{
    public class NetManager : MonoBehaviour
    {
        public NetPrefabRegistry NetPrefabs;
        private static NetManager _manager;
        private NetScene m_scene;
        public static ITransport Transport;
        public static int Port = 7777;
        public static List<int> allPlayers;
        private bool _isHost = false;
        private bool _running = false;
        
        [SerializeField] public string serverName = "Net_Server";
        [SerializeField] public int maxPlayers = 10;
        [SerializeField] public bool useLAN = false;
        [SerializeField] public bool debugLog = false;
        [SerializeField] public float lanDiscoveryInterval = 1f;
        private float _lastLanDiscovery;
        private List<ServerInfo> _discoveredServers = new List<ServerInfo>();
        
        public static bool IsHost => _manager._isHost;
        public static string ServerName => _manager.serverName;
        public static int MaxPlayers => _manager.maxPlayers;
        public static bool UseLan
        {
            get => _manager.useLAN;
            set => _manager.useLAN = value;
        }
        public static bool DebugLog
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
            if (m_scene == null) m_scene = new NetScene();
            if(NetPrefabs != null) NetScene.Instance.RegisterPrefabs(NetPrefabs.prefabs);
            Messager.RegisterHandler<RPCMessage>(RPCManager.CallRPC);
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
            Messager.ClearHandlers();
            NetScene.CleanUp();
        }

        public static void StartHost(ServerInfo serverInfo = null)
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            if(serverInfo != null) Transport.Setup(Port, serverInfo, _manager.debugLog);
            else Transport.Setup(Port, true, _manager.maxPlayers, _manager.debugLog);
            _manager._isHost = true;
            _manager._running = true;
            NetHost.StartHost();
            if (UseLan)
            {
                Transport.SetServerInfo(new ServerInfo(){ServerName = _manager.serverName, MaxPlayers = _manager.maxPlayers});
                Transport.BroadcastServerInfo();
            }
        }
        public static void StartClient()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false, _manager.maxPlayers, _manager.debugLog);
            _manager._isHost = false;
            _manager._running = true;
            if (!_manager.useLAN)
                NetClient.Connect(_manager.address);
            else
            {
                Transport.StartServerDiscovery();
                ITransport.OnLanServerUpdate += UpdateLanServers;
            }
        }


        public static void ConnectTo(IPEndPoint endPoint)
        {
            ConnectTo(endPoint.Address.ToString());
        }
        public static void ConnectTo(string address)
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false, _manager.maxPlayers, _manager.useLAN);
            _manager._isHost = false;
            NetClient.Connect(address);
        }
        public static void StopNet()
        {
            if (!_manager._running) return;
            if (IsHost) NetHost.Stop();
            else NetClient.Disconnect();
            allPlayers.Clear();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
            
            if (UseLan) StopLan();
            _manager._running = false;
        }

        public static int ConnectionId()
        {
            if (!IsHost) return NetClient.Connection.Id;
            return -1;
        }
        public static void Send(NetMessage netMessage)
        {
            if(IsHost)
                NetHost.Send(netMessage);
            else NetClient.Send(netMessage);
        }
        
        public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default, bool ownsPrefab = false)
        {
            SpawnMessage spm = new SpawnMessage(ConnectionId(), prefab.name, position, ownsPrefab);
            spm.requesterId = ConnectionId();
            if (IsHost)
            {
                NetScene.Instance.Spawn(spm);
            }
            else
            {
                NetClient.Send(spm);
            }
        }

        public static void Destroy(int netObjectId)
        {
            var netObj = NetScene.Instance.GetNetObject(netObjectId);
            if (netObj != null && (netObj.Owned || IsHost))
            {
                DestroyMessage msg = new DestroyMessage(netObjectId, ConnectionId());
                if (IsHost)
                {
                    NetScene.Instance.Destroy(netObjectId);
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
            return Transport.GetServerInfo();
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
            if (_manager._running && IsHost)
            {
                Transport.SetServerInfo(serverInfo);
            }
        }
        public static void SetServerName(string serverName)
        {
            if (_manager._running && IsHost)
            {
                ServerInfo serverInfo = Transport.GetServerInfo();
                serverInfo.ServerName = serverName;
                Transport.SetServerInfo(serverInfo);
            }
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
                Debug.Log("Received: " + msg);
                Messager.HandleMessage(msg);
            }
        }

        private static void UpdateLanServers(ServerInfo point)
        {
            Debug.Log("Detected Server: " + point.EndPoint.ToString());
            _manager._discoveredServers = Transport.GetDiscoveredServers();
        }
    }
}
