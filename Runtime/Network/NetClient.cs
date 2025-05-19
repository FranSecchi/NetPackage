using NetPackage.Messages;
using UnityEngine.SceneManagement;

namespace NetPackage.Network
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            Messager.RegisterHandler<SpawnMessage>(OnSpawned);;
            Messager.RegisterHandler<SceneLoadMessage>(OnSceneLoadMessage);
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
        }

        private static void OnSpawned(SpawnMessage obj)
        {
            if (obj.requesterId == Connection.Id)
            {
                NetScene.Instance.Reconciliate(obj);
            }
            else NetScene.Instance.Spawn(obj);
        }

        public static void Disconnect()
        {
            NetManager.Transport.Stop();
            Connection = null;
        }

        public static void Send(NetMessage netMessage)
        {
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
            if (!msg.isLoaded)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                NetManager.EnqueueMainThread(()=>SceneManager.LoadScene(msg.sceneName));
            }
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            SceneLoadMessage response = new SceneLoadMessage(scene.name, Connection.Id, true);
            Send(response);
        }
    }
}
