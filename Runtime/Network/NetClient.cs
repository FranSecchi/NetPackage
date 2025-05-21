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
            DebugQueue.AddNetworkMessage(netMessage, false);
            Connection?.Send(netMessage);
        }
        private static void OnConnected(ConnMessage connection)
        {
            if(Connection == null) Connection = new NetConn(connection.CurrentConnected, false);
            NetManager.allPlayers = connection.AllConnected;
            NetManager.SetServerInfo(connection.ServerInfo);
        }

    }
}
