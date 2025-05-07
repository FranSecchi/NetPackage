using System;
using System.Collections.Generic;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.NetPackage.Runtime.NetworkManager
{
    public class NetScene
    { 
        public static NetScene Instance;
        private Dictionary<string, GameObject> m_prefabs = new Dictionary<string, GameObject>();
        private Dictionary<int, NetObject> netObjects = new Dictionary<int, NetObject>();
        private Dictionary<long, GameObject> sceneObjects = new Dictionary<long, GameObject>();
        private int netObjectId = 0;

        public NetScene()
        {
            netObjectId = 0;
            Instance = this;
            Messager.RegisterHandler<OwnershipMessage>(OnOwnership);
        }

        private void OnOwnership(OwnershipMessage msg)
        {
            var netObj = GetNetObject(msg.netObjectId);
            if (netObj != null)
            {
                netObj.GiveOwner(msg.newOwnerId);
            }
        }

        public void RegisterPrefabs(List<GameObject> prefabs)
        {
            Debug.Log($"Registering {prefabs.Count} prefabs in NetScene instance {GetHashCode()}");
            foreach (var prefab in prefabs)
            {
                // prefab.GetComponent<NetBehaviour>().registered = true;
                m_prefabs[prefab.name] = prefab;
            }
        }

        public void RegisterSceneObject(NetBehaviour netBehaviour)
        {
            long sceneId = netBehaviour.GetComponent<SceneObjectId>().sceneId;
            if(!sceneObjects.ContainsKey(sceneId)) sceneObjects[sceneId] = netBehaviour.gameObject;
            if (!NetManager.IsHost) return;
            
            int id = netObjectId++;
            NetObject netObj = new NetObject(id, netBehaviour);
            Register(netObj);
        }
        public void Spawn(SpawnMessage msg)
        {
            if (msg.sceneId >= 0)
            {
                if (NetManager.IsHost) NetManager.EnqueueMainThread(() => { ValidateSpawn(msg);});
                else SpawnSceneObject(msg);
            }
            else
            {
                NetManager.EnqueueMainThread(() => { SpawnImmediate(msg);});
            }
        }

        private void SpawnImmediate(SpawnMessage msg)
        {
            if (NetManager.IsHost && msg.netObjectId >= 0) return;
            GameObject obj = m_prefabs[msg.prefabName];
            
            GameObject instance = GameObject.Instantiate(obj, msg.position, Quaternion.identity);
            NetObject netObj = instance.GetComponent<NetBehaviour>().NetObject;
            
            netObj.OwnerId = msg.own ? msg.requesterId : NetManager.ConnectionId();
            msg.netObjectId = msg.netObjectId >= 0 ? msg.netObjectId : netObj.NetId;
            Register(netObj);
            ValidateSpawn(msg);
            Debug.Log($"Spawned NetObject with ID {msg.netObjectId}, owned by {netObj.OwnerId}");
            msg.target = null;
            if(NetManager.IsHost)
                NetHost.Send(msg);
        }

        private void SpawnSceneObject(SpawnMessage msg)
        {
            if (sceneObjects.TryGetValue(msg.sceneId, out GameObject obj))
            {
                NetBehaviour netBehaviour = obj.GetComponent<NetBehaviour>();
                NetObject netObj = new NetObject(msg.netObjectId, netBehaviour, msg.own ? msg.requesterId : -1);
                Register(netObj);
                obj.transform.position = msg.position;
                NetManager.Send(msg);
            }
            else Debug.LogWarning($"A spawn request of a not found scene object has been received. Scene Id: {msg.sceneId} Requested by {msg.requesterId}");
        }

        private void ValidateSpawn(SpawnMessage msg)
        {
            Debug.Log("Validated spawn: "+msg.netObjectId);
            GetNetObject(msg.netObjectId)?.Enable();
        }

        public void Destroy(int objectId)
        {
            if (netObjects.TryGetValue(objectId, out NetObject obj))
            {
                obj.Destroy();
            }
            Unregister(objectId);
        }

        public NetObject GetNetObject(int netId)
        {
            return netObjects.TryGetValue(netId, out NetObject obj) ? obj : null;
        }

        public void Reconciliate(SpawnMessage spawnMessage)
        {
            //Compare
            
        }

        private void Register(NetObject obj)
        {
            if (netObjects.ContainsKey(obj.NetId)) return;
            netObjects[obj.NetId] = obj;
            StateManager.Register(obj.NetId, new ObjectState());
        }

        private void Unregister(int objectId)
        {
            netObjects.Remove(objectId);
            StateManager.Unregister(objectId);
        }

        public void SendObjects(int id)
        {
            Debug.Log("Sending Objects: " + sceneObjects.Count);
            foreach(var sceneObjects in sceneObjects)
            {
                GameObject obj = sceneObjects.Value.gameObject;
                NetManager.EnqueueMainThread(() => {
                    NetObject netObj = obj.GetComponent<NetBehaviour>().NetObject;
                    if (netObj == null) Debug.Log("IS NULL");
                
                    SpawnMessage msg = new SpawnMessage(
                        NetManager.ConnectionId(),
                        obj.name,
                        obj.transform.position,
                        own: true,
                        sceneId: sceneObjects.Key,
                        target: new List<int>{id});
                    msg.netObjectId = netObj.NetId;
                
                    NetHost.Send(msg); 
                });
                
            }
        }
    }
}
