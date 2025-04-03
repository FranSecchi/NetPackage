using System.Collections.Generic;
using System.Linq;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.NetworkManager
{
    public static class NetHost
    {
        public static Dictionary<int, NetConn> Clients = new Dictionary<int, NetConn>();
        private static readonly object Lock = new object();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            NetManager.allPlayers.Add(-1);
            ITransport.OnClientConnected += OnClientConnected;;
            ITransport.OnClientDisconnected += OnClientDisconnected;
        }

        private static void OnClientDisconnected(int id)
        {
            Debug.Log($"Client d");
            lock (Lock) // Ensure thread safety
            {
                Debug.Log($"Client d1");
                Clients.Remove(id);
                Debug.Log($"Client {id} disconnected. Clients count: {Clients.Count}");
                NetManager.allPlayers.Remove(id);
                UpdatePlayers(id);
            }
        }


        private static void OnClientConnected(int id)
        {
            lock (Lock) // Ensure thread safety
            {
                if (Clients.TryAdd(id, new NetConn(id, true))) // Thread-safe add
                {
                    Debug.Log($"Client {id} connected. Clients count: {Clients.Count}");
                    NetManager.allPlayers.Add(id);
                    UpdatePlayers(id);
                }
            }
        }

        private static void UpdatePlayers(int id)
        {
            NetMessage msg = new ConnMessage(null, id, NetManager.allPlayers);
            Send(msg);
        }

        public static void Stop()
        {
            foreach (KeyValuePair<int, NetConn> client in Clients)
            {
                client.Value.Disconnect();
            }
            NetManager.Transport.Disconnect();
            ITransport.OnClientConnected -= OnClientConnected;
            ITransport.OnClientDisconnected -= OnClientDisconnected;
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

        public static void Send(NetMessage netMessage)
        {
            if (netMessage.target == null)
            {
                foreach (int client in Clients.Keys)
                {
                    Clients[client].Send(netMessage);
                }
            }
            else
            {
                foreach (KeyValuePair<int, NetConn> client in Clients)
                {
                    if(netMessage.target.Contains(client.Key))
                        client.Value.Send(netMessage);
                }
            }
        }
        private static void OnSyncMessage(SyncMessage obj)
        {
            StateManager.SetSync(obj);
        }
    }
}
