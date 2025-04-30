using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
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
