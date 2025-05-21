using System;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
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
        private bool wasActive = false;
        
        protected virtual void Awake()
        {
            RegisterAsSceneObject();
        }

        public virtual void OnNetEnable()
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
                gameObject.SetActive(wasActive);
            }
            else 
                gameObject.SetActive(true);
        }
        public virtual void OnNetDisable()
        {
            if (NetObject != null)
            {
                StateManager.Unregister(NetObject.NetId, this);
                RPCManager.Unregister(NetObject.NetId, this);
            }
            enabled = false;
        }
        private void Start()
        {
            OnNetStart();
            // Register in play mode if not already registered
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
        }

        private void OnDestroy()
        {
            OnNetDisable();
            OnNetDestroy();
        }
        public void Disconnect()
        {
            if(!NetManager.IsHost)return;
            OnDisconnect();
        }

        protected virtual void OnNetSpawn(){ }
        protected virtual void OnNetStart(){}
        protected virtual void OnNetDestroy(){}
        protected virtual void OnDisconnect(){}

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
            var behaviours = gameObject.GetComponents<NetBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.NetObject != null)
                {
                    NetObject = behaviour.NetObject;
                    NetObject.Register(this);
                }
            }
            wasActive = enabled;
            if(wasActive) gameObject.SetActive(false);
            NetScene.RegisterSceneObject(this);
        }
        public void SetNetObject(NetObject obj)
        {
            if (obj == null) return;
            NetObject = obj;
            registered = true;
        }
    }
}
