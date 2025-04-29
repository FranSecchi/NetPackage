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
        private static NetManager _manager;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeNetBehaviours()
        {
            if (!IsHost) return;
            foreach (var netBehaviour in FindObjectsByType<NetBehaviour>(FindObjectsSortMode.None))
            {
                netBehaviour.PreAwakeInitialize();
            }
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
            SceneManager.sceneLoaded += NetScene.OnSceneLoaded;
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
            SceneManager.sceneLoaded -= NetScene.OnSceneLoaded;
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

        public static void Spawn(int prefabId, Vector3 position, Quaternion rotation = default(Quaternion), bool ownsPrefab = false)
        {
            if (IsHost)
            {
                NetScene.Spawn(prefabId, position, rotation);
            }
            else
            {
                // NetClient.Spawn(prefabId, position, rotation);
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
