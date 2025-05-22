using System;
using System.Collections.Generic;
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
        protected float _lastReconcileTime;
        protected float _predictionDelay = 0.1f;
        public bool IsPredicting => _isPredicting;
        
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
                RollbackManager.UpdatePrediction += UpdatePrediction;
            }
            if (!spawned)
            {
                spawned = true;
                if (NetManager.Rollback) 
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
                RollbackManager.UpdatePrediction += UpdatePrediction;
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
        
        private void UpdatePrediction()
        {
            if (!isOwned || !_isPredicting) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastPredictionTime >= _predictionDelay)
            {
                Predict(currentTime - _lastPredictionTime);
                _lastPredictionTime = currentTime;
            }
            else StopPrediction();
        }
        
        // Prediction methods
        protected virtual void StartPrediction()
        {
            if (!isOwned) return;
            _isPredicting = true;
            _lastPredictionTime = Time.time;
            OnPredictionStart();
        }
        
        protected void StopPrediction()
        {
            if (!isOwned) return;
            _isPredicting = false;
            OnPredictionStop();
        }
        
        protected virtual void OnPredictionStart() { }
        protected virtual void OnPredictionStop() { }
        
        // Called by StateManager when state changes are received
        public void OnStateReceived(int id, Dictionary<string, object> changes)
        {
            if (isOwned && _isPredicting)
            {
                // Check for desync with the new state changes
                if (IsDesynchronized(changes))
                {
                    // Log the desync
                    DebugQueue.AddRollback(NetID, _lastReconcileTime, "State desync detected at " + GetType().Name);
                    
                    // Trigger rollback
                    RollbackManager.RollbackToTime(NetID, id, _lastReconcileTime, changes);
                }
                else
                {
                    // Handle state reconciliation
                    _lastReconcileTime = Time.time;
                    OnStateReconcile(changes);
                }
            }
            else
            {
                // For non-owned objects, just apply the changes
                OnStateReconcile(changes);
            }
        }
        
        protected virtual void OnStateReconcile(Dictionary<string, object> changes) { }
        
        // New virtual methods for prediction and desync detection
        protected virtual void Predict(float deltaTime)
        {
            // Override this method to implement prediction logic
            // This is called during prediction phase
        }
        
        protected virtual bool IsDesynchronized(Dictionary<string, object> changes)
        {
            // Override this method to implement desync detection logic
            // Return true if the current state differs from the authoritative state
            return false;
        }
        
        
        
    }
}
