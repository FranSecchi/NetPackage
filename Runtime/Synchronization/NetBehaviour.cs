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
        public bool isOwned => NetObject.Owned;
        protected bool spawned = false;
        
        // Prediction support
        protected bool _isPredicting = false;
        protected float _lastPredictionTime;
        
        protected virtual void Awake()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
            RegisterAsSceneObject();
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
        private void Start()
        {
            // Register in play mode if not already registered
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
            OnNetStart();
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
                // Record the RPC for rollback if we're predicting
                if (_isPredicting)
                {
                    RollbackManager.RecordInput(NetID, methodName, parameters);
                }
                
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
        
        // New prediction methods
        protected virtual void StartPrediction()
        {
            if (!isOwned) return;
            _isPredicting = true;
            _lastPredictionTime = Time.time;
            OnPredictionStart();
        }
        
        protected virtual void StopPrediction()
        {
            if (!isOwned) return;
            _isPredicting = false;
            OnPredictionStop();
        }
        
        protected virtual void OnPredictionStart() { }
        protected virtual void OnPredictionStop() { }
        
        protected virtual void OnStateReceived()
        {
            if (isOwned && _isPredicting)
            {
                // Handle state reconciliation
                OnStateReconcile();
            }
        }
        
        protected virtual void OnStateReconcile() { }
        
        public bool IsPredicting => _isPredicting;
    }
}
