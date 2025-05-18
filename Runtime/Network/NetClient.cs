using NetPackage.Messages;

namespace NetPackage.Network
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            Messager.RegisterHandler<SpawnMessage>(OnSpawned);
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

        private static void OnConnected(ConnMessage connection)
        {
            Connection = new NetConn(connection.CurrentConnected, false);
            NetManager.allPlayers = connection.AllConnected;
        }

        public static void Send(NetMessage netMessage)
        {
            Connection?.Send(netMessage);
        }
        
    }
}
