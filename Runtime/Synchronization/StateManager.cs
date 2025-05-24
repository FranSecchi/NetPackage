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
        private static readonly ConcurrentDictionary<int, ObjectState> snapshot = new();
        private static readonly Dictionary<int, HashSet<object>> _componentCache = new();

        public static void Register(int netId, ObjectState state)
        {
            if (state == null) return;
            snapshot[netId] = state;
        }

        public static void Register(int netId, object obj)
        {
            if (obj == null) return;

            if (snapshot.TryGetValue(netId, out ObjectState state))
            {
                state.Register(netId, obj);
                
                // Cache component reference
                if (!_componentCache.TryGetValue(netId, out var components))
                {
                    components = new HashSet<object>();
                    _componentCache[netId] = components;
                }
                components.Add(obj);
            }
            else
            {
                DebugQueue.AddMessage("Not Registered state :" + obj.GetType().Name, DebugQueue.MessageType.State);
            }
        }

        public static void Unregister(int netId, object obj)
        {
            if (obj == null) return;

            if (snapshot.TryGetValue(netId, out ObjectState state))
            {
                state.Unregister(obj);
                
                // Remove from component cache
                if (_componentCache.TryGetValue(netId, out var components))
                {
                    components.Remove(obj);
                    if (components.Count == 0)
                    {
                        _componentCache.Remove(netId);
                    }
                }
            }
        }

        public static void Unregister(int netId)
        {
            snapshot.TryRemove(netId, out _);
            _componentCache.Remove(netId);
        }

        public static void Clear()
        {
            snapshot.Clear();
            _componentCache.Clear();
        }

        public static ObjectState GetState(int objectId)
        {
            return snapshot.TryGetValue(objectId, out ObjectState state) ? state : null;
        }

        public static Dictionary<int, ObjectState> GetAllStates()
        {
            return new Dictionary<int, ObjectState>(snapshot);
        }
        
        public static void RestoreState(int objectId, ObjectState state)
        {
            if (state == null) return;
            
            // If the object exists, update its state
            if (snapshot.TryGetValue(objectId, out ObjectState currentState))
            {
                // Update all tracked variables
                foreach (var kvp in state.TrackedSyncVars)
                {
                    var component = kvp.Key;
                    if (component == null) continue;

                    foreach (var fieldKvp in kvp.Value)
                    {
                        var field = fieldKvp.Key;
                        var value = fieldKvp.Value;
                        
                        try
                        {
                            field.SetValue(component, value);
                        }
                        catch (System.Exception e)
                        {
                            DebugQueue.AddMessage($"Failed to restore state for {component.GetType().Name}: {e.Message}", DebugQueue.MessageType.Error);
                        }
                    }
                }

                // Update the current state
                snapshot[objectId] = state.Clone();
            }
            else
            {
                // If the object doesn't exist, add it
                snapshot[objectId] = state.Clone();
            }
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
            var netObject = NetScene.GetNetObject(netObjectKey);
            
            if (netObject == null)
            {
                DebugQueue.AddMessage($"Failed to find NetObject {netObjectKey} for state update", DebugQueue.MessageType.Error);
                return;
            }

            foreach (var change in changes)
            {
                if (netObject.OwnerId == id)
                {
                    SyncMessage msg = new SyncMessage(id, netObjectKey, change.Key, change.Value);
                    NetManager.Send(msg);
                }
            }
        }

        public static void SetSync(SyncMessage syncMessage)
        {
            if (syncMessage == null) return;

            if (snapshot.TryGetValue(syncMessage.ObjectID, out ObjectState state))
            {
                if (syncMessage.SenderId == NetManager.ConnectionId())
                {
                    state.Reconcile(syncMessage.ObjectID, syncMessage.ComponentId, syncMessage.changedValues);
                }
                else 
                {
                    state.SetChange(syncMessage.ComponentId, syncMessage.changedValues);
                }
            }
            else 
            {
                DebugQueue.AddMessage(
                    $"Not SetSync: {syncMessage.ObjectID} Objects: {string.Join(", ", snapshot.Select(kv => $"{kv.Key}: {kv.Value}"))}",
                    DebugQueue.MessageType.Error
                );
            }
        }
    }
}
