using System.Collections.Generic;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using UnityEngine;

namespace Synchronization.NetPackage.Runtime.Synchronization
{
    public static class StateManager
    {
        //Object id - state
        private static Dictionary<int, ObjectState> snapshot = new();
        private static Dictionary<int, NetObject> _objects = new();
        public static void Register(NetObject netObject, ObjectState state)
        {
            if (netObject == null)
            {
                Debug.LogWarning("State object has to have a NetObject");
                return;
            }
            if (state == null)
            {
                Debug.LogWarning("State null");
                return;
            }
            snapshot[netObject.ObjectId] = state;
            _objects[netObject.ObjectId] = netObject;
        }
        public static ObjectState GetState(int objectId)
        {
            return snapshot.TryGetValue(objectId, out ObjectState state) ? state : null;
        }

        public static NetObject GetNetObject(int objectId)
        {
            return _objects.TryGetValue(objectId, out NetObject obj) ? obj : null;
        }
        public static void SendUpdateStates()
        {
            foreach (var netObject in snapshot)
            {
                var changes = netObject.Value.Update();
                if (changes.Count > 0)
                    Send(netObject.Key, changes);
            }
        }

        private static void Send(int netObjectKey, Dictionary<int, Dictionary<string, object>> changes)
        {
            foreach (var change in changes)
            {
                SyncMessage msg = new SyncMessage(netObjectKey, change.Key, change.Value);
                NetManager.Send(msg);
            }
        }

        public static void SetSync(SyncMessage syncMessage)
        {
            if (snapshot.TryGetValue(syncMessage.ObjectID, out ObjectState state))
            {
                state.SetChange(syncMessage.ComponentId, syncMessage.changedValues);
            }
        }
    }
}
