using System;
using System.Collections.Generic;
using System.Net;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.NetPackage.Runtime.NetworkManager
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
        
        [SerializeField] public bool useLAN = false;
        [SerializeField] public bool debugLog = false;
        [SerializeField] public float lanDiscoveryInterval = 1f;
        private float _lastLanDiscovery;
        private List<IPEndPoint> _discoveredServers = new List<IPEndPoint>();
        
        public static bool IsHost => _manager._isHost;
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

        public static void StartHost()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, true, _manager.useLAN, _manager.debugLog);
            _manager._isHost = true;
            _manager._running = true;
            NetHost.StartHost();
        }
        public static void StartClient()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false, _manager.useLAN, _manager.debugLog);
            _manager._isHost = false;
            _manager._running = true;
            if (!_manager.useLAN)
                NetClient.Connect(_manager.address);
            else
                ITransport.OnLanServerDiscovered += AddLanServer;
        }


        public static void ConnectTo(IPEndPoint endPoint)
        {
            ConnectTo(endPoint.Address.ToString());
        }
        public static void ConnectTo(string address)
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false, _manager.useLAN);
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
            if(UseLan && !IsHost) ITransport.OnLanServerDiscovered -= AddLanServer;
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

        public static List<IPEndPoint> GetDiscoveredServers()
        {
            return _manager._discoveredServers;
        }
        private static void AddLanServer(IPEndPoint point)
        {
            Debug.Log("Detected Server" + point.ToString());
            _manager._discoveredServers.Add(point);
        }
    }
}
