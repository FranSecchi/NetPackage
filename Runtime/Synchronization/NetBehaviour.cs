using System;
using NetPackage.Runtime.NetworkManager;
using UnityEngine;

namespace NetPackage.Runtime.Synchronization
{
    [RequireComponent(typeof(SceneObjectId))]
    public abstract class NetBehaviour : MonoBehaviour
    {
        [NonSerialized]
        public NetObject NetObject;
        public int NetID => NetObject.NetId;
        private bool registered = false;
        public  bool isOwned => NetObject.Owned;
        protected bool spawned = false;
        
        protected virtual void Awake()
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
            if (!spawned)
            {
                spawned = true;
                OnNetSpawn();
            }
            OnNetEnable();
        }

        public void OnDisable()
        {
            if (NetObject != null)
            {
                StateManager.Unregister(NetObject.NetId);
                RPCManager.Unregister(NetObject.NetId, this);
            }
            OnNetDisable();
        }
        protected virtual void Start()
        {
            // Register in play mode if not already registered
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
        }
        protected virtual void OnNetEnable(){ }
        protected virtual void OnNetDisable(){ }
        protected virtual void OnNetSpawn(){ }

        protected void CallRPC(string methodName, params object[] parameters)
        {
            if (NetObject != null)
            {
                RPCManager.SendRPC(NetObject.NetId, methodName, parameters);
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
