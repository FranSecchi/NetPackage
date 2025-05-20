using NetPackage.Messages;
using NetPackage.Synchronization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetPackage.Network
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            NetScene.Init();
            RPCManager.Init();
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            Messager.RegisterHandler<SpawnMessage>(OnSpawned);
            Messager.RegisterHandler<SyncMessage>(OnSync);
            Messager.RegisterHandler<SceneLoadMessage>(OnSceneLoadMessage);
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
        }

        private static void OnSync(SyncMessage obj)
        {
            StateManager.SetSync(obj);
        }

        private static void OnSpawned(SpawnMessage obj)
        {
            if (obj.requesterId == Connection.Id)
            {
                NetScene.Reconciliate(obj);
            }
            else NetScene.Spawn(obj);
        }

        public static void Disconnect()
        {
            NetManager.Transport.Stop();
            Connection = null;
        }

        public static void Send(NetMessage netMessage)
        {
            if(NetManager.DebugLog) Debug.Log($"Sending: {netMessage}");
            Connection?.Send(netMessage);
        }

        public static void LoadScene(string sceneName)
        {
            SceneLoadMessage msg = new SceneLoadMessage(sceneName, Connection.Id);
            Send(msg);
        }
        private static void OnConnected(ConnMessage connection)
        {
            Connection = new NetConn(connection.CurrentConnected, false);
            NetManager.allPlayers = connection.AllConnected;
            NetManager.SetServerInfo(connection.ServerInfo);
        }

        private static void OnSceneLoadMessage(SceneLoadMessage msg)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            NetManager.EnqueueMainThread(() =>
            {
                if(SceneManager.GetActiveScene().name != msg.sceneName)
                    SceneManager.LoadScene(msg.sceneName);
            });
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            SceneLoadMessage response = new SceneLoadMessage(scene.name, Connection.Id, true);
            Send(response);
        }
    }
}
