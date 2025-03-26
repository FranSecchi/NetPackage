using System.Collections.Generic;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public static class NetHost
    {
        public static Dictionary<int, NetConn> Clients = new Dictionary<int, NetConn>();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            ITransport.OnClientConnected += OnClientConnected;
        }

        private static void OnClientConnected(int id)
        {
            if(!Clients.ContainsKey(id))
            {
                Clients.Add(id, new NetConn(id, true));
            }
        }

        public static void Stop()
        {
            foreach (KeyValuePair<int, NetConn> client in Clients)
            {
                client.Value.Disconnect();
            }
            NetManager.Transport.Disconnect();
            ITransport.OnClientConnected -= OnClientConnected;
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
