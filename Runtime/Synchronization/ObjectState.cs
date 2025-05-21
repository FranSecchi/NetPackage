using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Sync : Attribute { }
    public class ObjectState
    {
        //Component_instance - (Var_info - Var_value)
        private Dictionary<object, Dictionary<FieldInfo, object>> _trackedSyncVars;
        private Dictionary<int, object> _objectIds;
        private int _nextId;
        public Dictionary<object, Dictionary<FieldInfo, object>> TrackedSyncVars => _trackedSyncVars;

        public Dictionary<int, object> ObjectIds => _objectIds;

        public ObjectState()
        {
            _trackedSyncVars = new();
            _objectIds = new();
            _nextId = 0;
        }
        public void Register(int netId, object obj)
        {
            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool hasSyncFields = false;

            foreach (FieldInfo field in fields)
            {
                if (Attribute.IsDefined(field, typeof(Sync)))
                {
                    hasSyncFields = true;
                    break;
                }
            }
            
            if (hasSyncFields)
            {
                if (!_trackedSyncVars.ContainsKey(obj))
                {
                    _trackedSyncVars[obj] = new Dictionary<FieldInfo, object>();
                    int id = _nextId++;
                    _objectIds[id] = obj;
                    DebugQueue.AddMessage($"Object with ID {netId}, component {obj.GetType().Name} with ID {id}", DebugQueue.MessageType.State);
                    DebugQueue.AddMessage($"Object {((NetBehaviour)obj).gameObject.name}", DebugQueue.MessageType.State);
                }
                foreach (FieldInfo field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(Sync)))
                    {
                        _trackedSyncVars[obj][field] = field.GetValue(obj);
                    }
                }
            }
        }
        //Dictionary of changed objects this frame with its changes
        public Dictionary<int, Dictionary<string, object>> Update()
        {

            Dictionary<int, Dictionary<string, object>> allChanges = new();
            Dictionary<object, Dictionary<FieldInfo, object>> updates = new();
            foreach (var obj in _objectIds)
            {
                var fields = _trackedSyncVars[obj.Value];
                Dictionary<string, object> changes = new Dictionary<string, object>();

                foreach (var fieldEntry in fields)
                {
                    FieldInfo field = fieldEntry.Key;
                    object oldValue = fieldEntry.Value;
                    object newValue = field.GetValue(obj.Value);

                    if (!Equals(oldValue, newValue))
                    {
                        DebugQueue.AddMessage($"SyncVar {field.Name} changed from {oldValue} to {newValue}", DebugQueue.MessageType.State);
                        changes[field.Name] = newValue;
                        if (!updates.ContainsKey(obj.Value))
                        {
                            updates[obj.Value] = new Dictionary<FieldInfo, object>();
                        }
                        updates[obj.Value][field] = newValue;
                    }
                }

                
                if (changes.Count > 0)
                {
                    allChanges[obj.Key] = changes;
                }
                foreach (var updateEntry in updates)
                {
                    foreach (var fieldUpdate in updateEntry.Value)
                    {
                        _trackedSyncVars[updateEntry.Key][fieldUpdate.Key] = fieldUpdate.Value;
                    }
                }
            }
            
            return allChanges;
        }

        public void SetChange(int netId, int id, Dictionary<string, object> changes)
        {
            if(!_objectIds.ContainsKey(id)) DebugQueue.AddMessage($"No object {netId} with component {id} found", DebugQueue.MessageType.Warning);
            
            DebugQueue.AddMessage($"SyncVar Update called: {netId} | {id}", DebugQueue.MessageType.State);
            object obj = _objectIds[id];
            foreach (var change in changes)
            {
                FieldInfo field = _trackedSyncVars[obj].Keys.FirstOrDefault(f => f.Name == change.Key);
                
                if (field != null && field.GetValue(obj) != change.Value)
                {
                    DebugQueue.AddStateChange(netId, id, change.Key, change.Value);
                    field.SetValue(obj, change.Value);
                }
            }
        }
        
        public Dictionary<FieldInfo, object> GetField(object obj)
        {
            if (_trackedSyncVars.TryGetValue(obj, out var field))
                return field;
            return null;
        }
        public ObjectState Clone()
        {
            ObjectState clone = new ObjectState();
            Dictionary<int, object> clonedIds = new();
            foreach (var obj in _objectIds)
            {
                var fields = _trackedSyncVars[obj.Value];
                Dictionary<FieldInfo, object> clonedFields = new Dictionary<FieldInfo, object>();
                clonedIds[obj.Key] = obj.Value;
                foreach (var fieldEntry in fields)
                {
                    clonedFields[fieldEntry.Key] = fieldEntry.Value;
                }
                
                clone._trackedSyncVars[obj.Value] = clonedFields;
            }
            clone._objectIds = clonedIds;
            return clone;
        }

        public void Unregister(object o)
        {
            _trackedSyncVars.Remove(o);

            int? keyToRemove = null;
            foreach (var pair in _objectIds)
            {
                if (ReferenceEquals(pair.Value, o))
                {
                    keyToRemove = pair.Key;
                    break;
                }
            }

            if (keyToRemove.HasValue)
            {
                _objectIds.Remove(keyToRemove.Value);
            }
        }
    }
}
