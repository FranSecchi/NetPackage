using System.Collections.Generic;
using System.Linq;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using System.Net;

namespace NetworkManagerTest
{
    public class NetManagerTest : MonoBehaviour
    {
        private static NetManagerTest _manager;
        public static ITransport Transport;
        public static int Port = 7777;
        public static List<int> allPlayers;
        private bool _isHost = false;
        private bool _running = false;
        
        [SerializeField] public string ServerName = "Net_Server";
        [SerializeField] public int maxPlayers = 10;
        [SerializeField] public bool useLAN = false;
        [SerializeField] public bool debugLog = false;
        [SerializeField] public float lanDiscoveryInterval = 1f;
        private float _lastLanDiscovery;
        private List<ServerInfo> _discoveredServers = new List<ServerInfo>();
        
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
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            if (useLAN && !IsHost)
            {
                if (Time.time - _lastLanDiscovery >= lanDiscoveryInterval)
                {
                    _discoveredServers = Transport.GetDiscoveredServers();
                    _lastLanDiscovery = Time.time;
                }
            }
        }

        public static void StartHost()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, true, _manager.debugLog);
            _manager._isHost = true;
            _manager._running = true;
            NetHostTest.StartHost();
            if (UseLan)
            {
                Transport.SetServerInfo(new ServerInfo(){ServerName = _manager.ServerName, MaxPlayers = _manager.maxPlayers});
                Transport.BroadcastServerInfo();
            }
        }
        public static void StartClient()
        {
            StopNet();
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false, _manager.debugLog);
            _manager._isHost = false;
            _manager._running = true;
            if (!_manager.useLAN)
            {
                NetClientTest.Connect(_manager.address);
            }
            else
            {
                Transport.StartServerDiscovery();
                ITransport.OnLanServerDiscovered += AddLanServer;
            }
        }

        private static void AddLanServer(ServerInfo point)
        {
            Debug.Log("Detected Server" + point.ToString());
            _manager._discoveredServers.Add(point);
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
            NetClientTest.Connect(address);
        }

        public static void StopNet()
        {
            if (!_manager._running) return;
            if (IsHost) NetHostTest.Stop();
            else NetClientTest.Disconnect();
            allPlayers.Clear();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
            if (UseLan) StopLan();
            _manager._running = false;
        }

        public static void StopLan()
        {
            if (!IsHost)
            {
                ITransport.OnLanServerDiscovered -= AddLanServer;
                Transport.StopServerDiscovery();
            }
            else Transport.StopServerBroadcast();
        }
        public static void Send(NetMessage netMessage)
        {
            if(IsHost)
                NetHostTest.Send(netMessage);
            else NetClientTest.Send(netMessage);
        }

        private static void Receive(int id)
        {
            byte[] data = Transport.Receive();
            
            if (data != null && data.Length != 0)
            {
                NetMessage msg = NetSerializer.Deserialize<NetMessage>(data);
                Messager.HandleMessage(msg);
            }
        }

        public static List<ServerInfo> GetDiscoveredServers()
        {
            return _manager._discoveredServers;
        }
    }
    public static class NetClientTest
    {
        public static NetConnTest Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            NetManagerTest.Transport.Start();
            NetManagerTest.Transport.Connect(address);
        }
        public static void Disconnect()
        {
            NetManagerTest.Transport.Stop();
            Connection = null;
        }

        private static void OnConnected(ConnMessage connection)
        {
            if(Connection != null) Connection = new NetConnTest(connection.CurrentConnected, false);
            NetManagerTest.allPlayers = connection.AllConnected;
        }

        public static void Send(NetMessage netMessage)
        {
            NetManagerTest.Transport.Send(NetSerializer.Serialize(netMessage));
        }
    }
    public static class NetHostTest
    {
        public static Dictionary<int, NetConnTest> Clients = new Dictionary<int, NetConnTest>();
        private static readonly object Lock = new object();
        public static void StartHost()
        {
            NetManagerTest.Transport.Start();
            NetManagerTest.allPlayers.Add(-1);
            ITransport.OnClientConnected += OnClientConnected;
            ITransport.OnClientDisconnected += OnClientDisconnected;
        }
        
        
        private static void OnClientConnected(int id)
        {
            lock (Lock) // Ensure thread safety
            {
                if (Clients.TryAdd(id, new NetConnTest(id, true))) // Thread-safe add
                {
                    Debug.Log($"Client {id} connected. Clients count: {Clients.Count}");
                    NetManagerTest.allPlayers.Add(id);
                    UpdatePlayers(id);
                }
            }
        }

        private static void OnClientDisconnected(int id)
        {
            lock (Lock) // Ensure thread safety
            {
                Clients.Remove(id);
                Debug.Log($"Client {id} disconnected. Clients count: {Clients.Count}");
                NetManagerTest.allPlayers.Remove(id);
                UpdatePlayers(id);
            }
        }
        private static void UpdatePlayers(int id)
        {
            NetMessage msg = new ConnMessage(id, NetManagerTest.allPlayers);
            Send(msg);
        }
        public static void Stop()
        {
            lock (Lock) // Ensure thread safety
            {
                foreach (KeyValuePair<int, NetConnTest> client in Clients)
                {
                    client.Value.Disconnect();
                }

                NetManagerTest.Transport.Stop();
                ITransport.OnClientConnected -= OnClientConnected;
                ITransport.OnClientDisconnected -= OnClientDisconnected;
                Clients.Clear();
            }
        }

        public static void Kick(int id)
        {
            lock (Lock) // Ensure thread safety
            {
                if (Clients.TryGetValue(id, out NetConnTest client))
                {
                    client.Disconnect();
                    Clients.Remove(id);
                }
            }
        }

        public static void Send(NetMessage netMessage)
        {

            lock (Lock) // Ensure thread safety
            {
                if (netMessage.target == null)
                {
                    foreach (int client in Clients.Keys)
                    {
                        Clients[client].Send(netMessage);
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, NetConnTest> client in Clients)
                    {
                        if (netMessage.target.Contains(client.Key))
                            client.Value.Send(netMessage);
                    }
                }
            }
        }
    }
    public class NetConnTest
    {
        public int Id { get; private set; }
        public bool IsHost { get; private set; }
        private readonly ITransport _transport;
        public NetConnTest(int id, bool isHost)
        {
            Id = id;
            IsHost = isHost;
            _transport = NetManagerTest.Transport;
        }

        public void Disconnect()
        {
            _transport?.Kick(Id);
        }
        public void Send(NetMessage netMessage)
        {
            _transport.SendTo(Id,NetSerializer.Serialize(netMessage));
        }
    }
}
