using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using NetPackage.Network;
using NetPackage.Messages;
using NetPackage.Utilities;
using UnityEngine;

namespace NetPackage.Synchronization
{
    internal class StateManager
    {
        //Object id - state
        private static ConcurrentDictionary<int, ObjectState> snapshot = new();
        public static void Register(int netId, ObjectState state)
        {
            if (state == null)
            {
                return;
            }
            snapshot[netId] = state;
        }
        public static void Register(int netId, object obj)
        {
            if (snapshot.TryGetValue(netId, out ObjectState state))
            {
                state.Register(netId, obj);
            }
            else
                DebugQueue.AddMessage("Not Registered state :" + obj.GetType().Name, DebugQueue.MessageType.State);
        }
        public static void Unregister(int netId, object obj)
        {
            if (snapshot.TryGetValue(netId, out ObjectState state))
            {
                state.Unregister(obj);
            }
        }

        public static void Unregister(int netId)
        {
            snapshot.TryRemove(netId, out _);
        }
        public static void Clear()
        {
            snapshot.Clear();
        }
        public static ObjectState GetState(int objectId)
        {
            return snapshot.TryGetValue(objectId, out ObjectState state) ? state : null;
        }

        public static void SendUpdateStates()
        {
            foreach (var netObject in snapshot)
            {
                if (netObject.Value == null) continue;
                
                var changes = netObject.Value.Update();
                if (changes.Count > 0)
                {
                    Send(netObject.Key, changes);
                }
            }
        }

        private static void Send(int netObjectKey, Dictionary<int, Dictionary<string, object>> changes)
        {
            int id = NetManager.ConnectionId();
            foreach (var change in changes)
            {
                if(NetScene.GetNetObject(netObjectKey).OwnerId == id)
                {
                    SyncMessage msg = new SyncMessage(id, netObjectKey, change.Key, change.Value);
                    NetManager.Send(msg);
                }
            }
        }

        public static void SetSync(SyncMessage syncMessage)
        {
            if (syncMessage.SenderId == NetManager.ConnectionId())
            {
                //Reconcile
            }
            else if (snapshot.TryGetValue(syncMessage.ObjectID, out ObjectState state))
            {
                state.SetChange(syncMessage.ComponentId, syncMessage.changedValues);
            }
            else DebugQueue.AddMessage(
                $"Not SetSync: {syncMessage.ObjectID} Objects: {string.Join(", ", snapshot.Select(kv => $"{kv.Key}: {kv.Value}"))}",
                DebugQueue.MessageType.Error
            );
        }
    }
}
