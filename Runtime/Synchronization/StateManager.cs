using System.Collections.Generic;
using System.Linq;
using NetPackage.Network;
using NetPackage.Messages;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public static class StateManager
    {
        //Object id - state
        private static Dictionary<int, ObjectState> snapshot = new();
        public static void Register(int netId, ObjectState state)
        {

            if (state == null)
            {
                return;
            }
            snapshot[netId] = state;
            DebugQueue.AddMessage("Register " + netId + " Count: " + snapshot.Count, DebugQueue.MessageType.State);
        }
        public static void Register(int netId, object obj)
        {
            DebugQueue.AddMessage("Register behavior " + obj.GetType(), DebugQueue.MessageType.State);

            if (snapshot.TryGetValue(netId, out ObjectState state))
            {
                state.Register(netId, obj);
            }
            else
                DebugQueue.AddMessage("Not Registered :" + obj.GetType(), DebugQueue.MessageType.State);

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
            snapshot.Remove(netId);
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
                    Send(netObject.Key, changes);
            }
        }

        private static void Send(int netObjectKey, Dictionary<int, Dictionary<string, object>> changes)
        {
            // int id = NetManager.ConnectionId();
            foreach (var change in changes)
            {
                // if(id == -1 || NetScene.GetNetObject(change.Key).OwnerId == id)
                // {
                    SyncMessage msg = new SyncMessage(netObjectKey, change.Key, change.Value);
                    NetManager.Send(msg);
                // }
            }
        }

        public static void SetSync(SyncMessage syncMessage)
        {
            DebugQueue.AddMessage("Message setsync " + syncMessage, DebugQueue.MessageType.State);
            if (snapshot.TryGetValue(syncMessage.ObjectID, out ObjectState state))
            {
                state.SetChange(syncMessage.ObjectID, syncMessage.ComponentId, syncMessage.changedValues);
            }
            else DebugQueue.AddMessage(
                $"Not SetSync: {syncMessage.ObjectID} Objects: {string.Join(", ", snapshot.Select(kv => $"{kv.Key}: {kv.Value}"))}",
                DebugQueue.MessageType.Error
            );}
    }
}
