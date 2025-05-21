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
        
        private void Awake()
        {
            Init();
            RegisterAsSceneObject();
        }
        private void OnEnable()
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
                StateManager.Unregister(NetObject.NetId, this);
                RPCManager.Unregister(NetObject.NetId, this);
            }
            OnNetDisable();
        }
        private void Start()
        {
            // Register in play mode if not already registered
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
        }

        public void Disconnect()
        {
            if(!NetManager.IsHost)return;
            OnDisconnect();
        }

        protected virtual void Init(){}
        protected virtual void OnNetEnable(){ }
        protected virtual void OnNetDisable(){ }
        protected virtual void OnNetSpawn(){ }
        public virtual void OnDisconnect(){}

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
            if(gameObject.activeSelf) gameObject.SetActive(false);
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
