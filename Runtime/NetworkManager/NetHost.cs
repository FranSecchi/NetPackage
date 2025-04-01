using System;
using System.Collections.Generic;
using System.Linq;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public static class NetHost
    {
        public static Dictionary<int, NetConn> Clients = new Dictionary<int, NetConn>();
        private static readonly object Lock = new object();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            ITransport.OnClientConnected += OnClientConnected;
        }


        private static void OnClientConnected(int id)
        {
            lock (Lock) // Ensure thread safety
            {
                if (Clients.TryAdd(id, new NetConn(id, true))) // Thread-safe add
                {
                    Debug.Log($"Client {id} connected. Clients count: {Clients.Count}");
                }
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
    }
}
