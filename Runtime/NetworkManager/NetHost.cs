using System.Collections.Generic;
using Transport.NetPackage.Runtime.Transport;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetHost
    {
        public static readonly Dictionary<int, NetConn> Clients = new Dictionary<int, NetConn>();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            ITransport.OnClientConnected += OnClientConnected;
        }

        private static void OnClientConnected(int id)
        {
            Clients[id] = new NetConn(id);
        }

        public static void Stop()
        {
            foreach (KeyValuePair<int, NetConn> client in Clients)
            {
                client.Value.Disconnect();
            }
            NetManager.Transport.Disconnect();
            Clients.Clear();
        }

        public static void Kick(int id)
        {
            if (Clients.TryGetValue(id, out NetConn client))
            {
                client.Disconnect();
                Clients.Remove(id);
            }
        }
    }
}
