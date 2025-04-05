using System.Collections.Generic;
using System.Linq;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;

namespace NetworkManagerTest
{
    public class NetManagerTest :MonoBehaviour
    {
        private static NetManagerTest _manager;
        public static ITransport Transport;
        public static int Port = 9050;
        public static List<int> allPlayers;
        private static bool IsHost = false;
        
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
        public static void StartHost()
        {
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, true);
            IsHost = true;
            NetHostTest.StartHost();
        }
        public static void StopHosting()
        {
            if (!IsHost) return;
            NetHostTest.Stop();
            StopNet();
        }
        public static void StartClient()
        {
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false);
            IsHost = false;
            NetClientTest.Connect(_manager.address);
        }
        public static void StopClient()
        {
            if (IsHost) return;
            NetClientTest.Disconnect();
            StopNet();
        }

        private static void StopNet()
        {
            allPlayers.Clear();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
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
                Messager.HandleMessage(data);
            }
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
