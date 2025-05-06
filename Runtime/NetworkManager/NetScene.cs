using System;
using System.Collections.Generic;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
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


        public void RegisterPrefabs(List<GameObject> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                m_prefabs[prefab.name] = prefab;
            }
        }
        public void RegisterSceneObject(NetBehaviour netBehaviour)
        {
            long sceneId = netBehaviour.GetComponent<SceneObjectId>().sceneId;
            sceneObjects[sceneId] = netBehaviour.gameObject;
            
            if (!NetManager.IsHost) return;
            int id = netObjectId++;
            NetObject netObj = new NetObject(id, netBehaviour);
            Register(netObj);
            SpawnMessage msg = new SpawnMessage(
                                    NetManager.ConnectionId(),
                                    netBehaviour.name,
                                    netBehaviour.transform.position,
                                    sceneId: sceneId);
            msg.netObjectId = id;
            NetHost.Send(msg);
        }
        
        public int Spawn(SpawnMessage msg)
        {
            if (msg.sceneId >= 0)
            {
                if (sceneObjects.TryGetValue(msg.sceneId, out GameObject obj))
                {
                    NetBehaviour netBehaviour = obj.GetComponent<NetBehaviour>();
                    NetObject netObj = new NetObject(msg.netObjectId, netBehaviour);
                    Register(netObj);
                    obj.transform.position = msg.position;
                }
            }
            else
            {
                GameObject obj = m_prefabs[msg.prefabName];
                msg.netObjectId = msg.netObjectId >= 0 ? msg.netObjectId : netObjectId++;
                
                NetManager.EnqueueMainThread(() =>
                {
                    GameObject instance = GameObject.Instantiate(obj, msg.position, Quaternion.identity);
                    NetObject netObj = new NetObject(msg.netObjectId, instance.GetComponent<NetBehaviour>(), msg.requesterId);
                    Register(netObj);
                    Debug.Log($"Spawned NetObject with ID {msg.netObjectId}");
                });
            }
            return msg.netObjectId;
        }
        public void Destroy(int objectId)
        {
            // BroadcastDestroy(objectId);
            Unregister(objectId);
        }

        public void Reconciliate(SpawnMessage spawnMessage)
        {
            //Compare
            
        }
        private void Register(NetObject obj)
        {
            netObjects[obj.NetId] = obj;
            StateManager.Register(obj.NetId, new ObjectState());
        }
        private void Unregister(int objectId)
        {
            netObjects.Remove(objectId);
            StateManager.Unregister(objectId);
        }

    }
}
