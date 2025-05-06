using System;
using System.Collections.Generic;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
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
        public static int Port = 9050;
        public static List<int> allPlayers;
        private bool _isHost = false;
        
        public static bool IsHost => _manager._isHost;
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
            if(NetScene.Instance == null) NetScene.Instance = m_scene;
            if(NetPrefabs != null) NetScene.Instance.RegisterPrefabs(NetPrefabs.prefabs);
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
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, true);
            _manager._isHost = true;
            NetHost.StartHost();
        }
        public static void StopHosting()
        {
            if (!IsHost) return;
            NetHost.Stop();
            StopNet();
        }
        public static void StartClient()
        {
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false);
            _manager._isHost = false;
            NetClient.Connect(_manager.address);
        }
        public static void StopClient()
        {
            if (IsHost) return;
            NetClient.Disconnect();
            StopNet();
        }

        private static void StopNet()
        {
            allPlayers.Clear();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
            // SceneManager.sceneLoaded -= NetScene.OnSceneLoaded;
        }
        public static int ConnectionId()
        {
            if (!IsHost) return NetClient.Connection.Id;
            return 0;
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
                spm.netObjectId = NetScene.Instance.Spawn(spm);
                NetHost.Send(spm);
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
                Messager.HandleMessage(msg);
            }
        }
    }
}
