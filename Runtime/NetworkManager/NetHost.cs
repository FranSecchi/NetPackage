using System.Collections.Generic;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetHost
    {
        public static readonly Dictionary<int, NetConn> Clients = new Dictionary<int, NetConn>();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            NetManager.Transport.OnClientConnected += OnClientConnected;
        }

        private static void OnClientConnected(int id)
        {
            Clients[id] = new NetConn(id);
        }
    }
}
