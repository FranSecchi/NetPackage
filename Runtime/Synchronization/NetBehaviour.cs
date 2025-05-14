using System;
using Runtime.NetPackage.Runtime.NetworkManager;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    [RequireComponent(typeof(SceneObjectId))]
    public abstract class NetBehaviour : MonoBehaviour
    {
        [NonSerialized]
        public NetObject NetObject;
        private bool registered = false;
        public bool isOwned = true;
        protected bool spawned;
        
        private void Awake()
        {
            RegisterAsSceneObject();
        }

        public void OnEnable()
        {
            if (NetObject != null)
            {
                StateManager.Register(NetObject.NetId, this);
                RPCManager.Register(NetObject.NetId, this);
            }
        }

        public void OnDisable()
        {
            if (NetObject != null)
            {
                StateManager.Unregister(NetObject.NetId);
                RPCManager.Unregister(NetObject.NetId, this);
            }
        }

        protected void CallRPC(string methodName, params object[] parameters)
        {
            if (NetObject != null)
            {
                RPCManager.SendRPC(NetObject.NetId, methodName, parameters);
                if(isOwned)
                    RPCManager.CallRPC(NetObject.NetId, methodName, parameters);
            }
        }

        private void Start()
        {
            // Register in play mode if not already registered
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
        }

        private void RegisterAsSceneObject()
        {
            if (registered) return;
            registered = true;
            if (gameObject.TryGetComponent<NetBehaviour>(out var obj) && obj.NetObject != null)
            {
                NetObject = obj.NetObject;
                NetObject.Register(this);
                return;
            }
            if(gameObject.activeSelf) gameObject.SetActive(false);
            NetScene.Instance?.RegisterSceneObject(this);
        }
        public void SetNetObject(NetObject obj)
        {
            if (obj == null) return;
            NetObject = obj;
            registered = true;
        }
    }
}
