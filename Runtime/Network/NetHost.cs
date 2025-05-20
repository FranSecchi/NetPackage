using System.Collections.Concurrent;
using NetPackage.Messages;
using NetPackage.Synchronization;
using NetPackage.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetPackage.Network
{
    public static class NetHost
    {
        public static ConcurrentDictionary<int, NetConn> Clients = new();
        private static readonly object Lock = new object();
        public static void StartHost()
        {
            NetManager.Transport.Start();
            NetManager.allPlayers.Add(-1);
            NetScene.Init();
            RPCManager.Init();
            ITransport.OnClientConnected += OnClientConnected;;
            ITransport.OnClientDisconnected += OnClientDisconnected;
            Messager.RegisterHandler<SyncMessage>(OnSyncMessage);
            Messager.RegisterHandler<SpawnMessage>(OnSpawnMessage);
            Messager.RegisterHandler<ConnMessage>(OnConnMessage);
        }

        private static void OnConnMessage(ConnMessage obj)
        {
            if(!obj.AllConnected.Count.Equals(NetManager.allPlayers.Count))
                UpdatePlayers(obj.CurrentConnected);
        }


        private static void OnClientDisconnected(int id)
        {
            if(Clients.TryRemove(id, out _))
            {
                Debug.Log($"Client {id} disconnected. Clients count: {Clients.Count}");
                NetManager.allPlayers.Remove(id);
                UpdatePlayers(id);
            }
        }


        private static void OnClientConnected(int id)
        {
            if (Clients.TryAdd(id, new NetConn(id, true)))
            {
                Debug.Log($"Client {id} connected. Clients count: {Clients.Count}");
                NetManager.allPlayers.Add(id);
                UpdatePlayers(id);
                //NetScene.SendScene(id);
            }
        }

        public static void UpdatePlayers(int id)
        {
            if (Clients.Count == 0) return;
            NetMessage msg = new ConnMessage(id, NetManager.allPlayers, NetManager.ServerInfo);
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
            if (Clients.TryGetValue(id, out NetConn client))
            {
                client.Disconnect();
            }
        }

        public static void Send(NetMessage netMessage)
        {
            if (netMessage.target == null)
            {
                if(NetManager.DebugLog) Debug.Log($"Sending all: {netMessage}");
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
                        if(NetManager.DebugLog) Debug.Log($"Sending to {targetId}: {netMessage}");
                        client.Send(netMessage);
                    }
                }
            }
        }
        private static void OnSyncMessage(SyncMessage obj)
        {
            StateManager.SetSync(obj);
        }
        
        private static void OnSpawnMessage(SpawnMessage msg)
        {
            //Validate
            NetScene.Spawn(msg);
        }

    }
}
