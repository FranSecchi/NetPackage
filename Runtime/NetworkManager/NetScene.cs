using System.Collections.Generic;
using Runtime.NetPackage.Runtime.Synchronization;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.NetPackage.Runtime.NetworkManager
{
    public class NetScene
    { 
        private static NetScene m_NetScene;
        private Dictionary<int, NetObject> netObjects;
        private int netObjectId = 0;

        public static void Register(NetObject obj)
        {
            m_NetScene.netObjects[obj.NetId] = obj;
            StateManager.Register(obj.NetId, new ObjectState());
        }

        public static void Unregister(int objectId)
        {
            m_NetScene.netObjects.Remove(objectId);
            StateManager.Unregister(objectId);
        }

        public static void Spawn(int prefabId, Vector3 pos, Quaternion rot)
        {
                // NetPrefabRegistry.Create(prefabId, pos, rot);
            // NetObject obj = new NetObject(netObjectId++);
            // Register(obj);
            // BroadcastSpawn(obj);
        }
        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // foreach (NetBehaviour netBehaviour in FindObjectsByType<NetBehaviour>(FindObjectsSortMode.InstanceID))
            // {
            //     if (netBehaviour.NetObject == null)
            //     {
            //         // NetObject netObj = new NetObject(m_NetScene.netObjectId++);
            //         // netBehaviour.AssignNetObject(netObj);
            //         // Register(netObj);
            //     }
            // }
        }
        public static void Destroy(int objectId)
        {
            // BroadcastDestroy(objectId);
            Unregister(objectId);
        }
    }
}
