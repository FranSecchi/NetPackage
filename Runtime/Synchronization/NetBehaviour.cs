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
        
        protected virtual void Awake()
        {
            if(GetComponent<SceneObjectId>().sceneId != "") RegisterAsSceneObject();
        }
        protected virtual void OnEnable()
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

        protected virtual void OnDisable()
        {
            if (NetObject != null)
            {
                StateManager.Unregister(NetObject.NetId, this);
                RPCManager.Unregister(NetObject.NetId, this);
            }
            OnNetDisable();
        }
        public void Disconnect()
        {
            if(!NetManager.IsHost)return;
            OnDisconnect();
        }

        protected virtual void OnNetStart(){}
        protected virtual void OnNetEnable(){ }
        protected virtual void OnNetDisable(){ }
        protected virtual void OnNetSpawn(){ }
        protected virtual void OnDisconnect(){}

        protected void CallRPC(string methodName, params object[] parameters)
        {
            if (NetObject != null)
            {
                RPCManager.SendRPC(NetObject.NetId, methodName, parameters);
            }
        }

        public void Own(int ownerId, bool ownChildren = false)
        {
            if(NetObject == null) return;
            NetObject.GiveOwner(ownerId);
            if (ownChildren)
            {
                var childs = GetComponentsInChildren<NetBehaviour>();
                foreach (var child in childs)
                {
                    child.Own(ownerId, true);
                }
            }
        }
        private void RegisterAsSceneObject()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
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
            if(isActiveAndEnabled) enabled = false;
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
