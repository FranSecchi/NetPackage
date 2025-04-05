using System.Collections.Concurrent;
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
        public static ConcurrentDictionary<int, NetConn> Clients = new();
        private static readonly object Lock = new object();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            NetManager.allPlayers.Add(-1);
            ITransport.OnClientConnected += OnClientConnected;;
            ITransport.OnClientDisconnected += OnClientDisconnected;
            Messager.RegisterHandler<SyncMessage>(OnSyncMessage);
        }

        private static void OnClientDisconnected(int id)
        {
            Clients.TryRemove(id, out _);
            Debug.Log($"Client {id} disconnected. Clients count: {Clients.Count}");
            NetManager.allPlayers.Remove(id);
            UpdatePlayers(id);
        }


        private static void OnClientConnected(int id)
        {
            if (Clients.TryAdd(id, new NetConn(id, true))) // Thread-safe add
            {
                Debug.Log($"Client {id} connected. Clients count: {Clients.Count}");
                NetManager.allPlayers.Add(id);
                UpdatePlayers(id);
            }
        }

        private static void UpdatePlayers(int id)
        {
            NetMessage msg = new ConnMessage(id, NetManager.allPlayers);
            Send(msg);
        }

        public static void Stop()
        {
            foreach (var client in Clients.Values)
            {
                client.Disconnect();
            }

            NetManager.Transport.Stop();
            ITransport.OnClientConnected -= OnClientConnected;
            ITransport.OnClientDisconnected -= OnClientDisconnected;
            Clients.Clear();
        }

        public static void Kick(int id)
        {
            if (Clients.TryRemove(id, out NetConn client))
            {
                client.Disconnect();
            }
        }

        public static void Send(NetMessage netMessage)
        {
            if (netMessage.target == null)
            {
                foreach (var client in Clients.Values)
                {
                    client.Send(netMessage);
                }
            }
            else
            {
                foreach (int targetId in netMessage.target)
                {
                    if (Clients.TryGetValue(targetId, out NetConn client))
                    {
                        client.Send(netMessage);
                    }
                }
            }
        }
        private static void OnSyncMessage(SyncMessage obj)
        {
            StateManager.SetSync(obj);
        }
    }
}
