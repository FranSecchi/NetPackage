using System;
using System.Collections.Generic;
using NetPackage.Network;
using NetPackage.Utilities;
using UnityEngine;

namespace NetPackage.Synchronization
{
    /// <summary>
    /// Base class for network-enabled behaviours that can be synchronized across the network.
    /// </summary>
    [RequireComponent(typeof(SceneObjectId))]
    public abstract class NetBehaviour : MonoBehaviour
    {
        /// <summary>
        /// The network object associated with this behaviour. Behaviours in the same GameObject return the same NetObject.
        /// </summary>
        [NonSerialized]
        public NetObject NetObject;

        /// <summary>
        /// Gets the network ID of this behaviour's associated network object.
        /// </summary>
        public int NetID => NetObject.NetId;
        
        /// <summary>
        /// Gets whether this behaviour is owned by the local client.
        /// </summary>
        public bool isOwned => NetObject.Owned;

        /// <summary>
        /// Indicates whether this behaviour has been spawned across the network.
        /// </summary>
        protected bool spawned = false;
        
        private bool registered = false;
        // Prediction support
        protected bool _isPredicting = false;
        protected float _lastPredictionTime;
        protected float _lastReconcileTime;
        protected float _predictionDelay = 0.1f;
        public bool IsPredicting => _isPredicting;
        /// <summary>
        /// Override - use only for declaring and initializing, network calls are not consistent.
        /// </summary>
        protected virtual void Awake()
        {
            if(GetComponent<SceneObjectId>().SceneId != "") RegisterAsSceneObject();
        }
        private void OnEnable()
        {
            if (NetObject == null)
                return;
                StateManager.Register(NetObject.NetId, this);
                RPCManager.Register(NetObject.NetId, this);
                RollbackManager.UpdatePrediction += UpdatePrediction;
            if (!spawned)
            {
                DebugQueue.AddMessage($"Spawned {GetType().Name} | {gameObject.name}.", DebugQueue.MessageType.Warning);

                spawned = true;
                if (NetManager.Rollback) 
                OnNetSpawn();
            }
            else OnNetEnable();
        }

        private void OnDisable()
        {
            if (NetObject == null)
                return;
            StateManager.Unregister(NetObject.NetId, this);
            RPCManager.Unregister(NetObject.NetId, this);
            RollbackManager.UpdatePrediction += UpdatePrediction;
            OnNetDisable();
        }

        private void Start()
        {
            if (!registered && NetObject == null)
            {
                RegisterAsSceneObject();
            }
            OnNetStart();
        }

        internal void Disconnect()
        {
            if(!NetManager.IsHost)return;
            OnDisconnect();
        }

        /// <summary>
        /// Called the frame after enabling the behaviour. Use it as you would use the default Start event.
        /// </summary>
        protected virtual void OnNetStart(){}

        /// <summary>
        /// Called the first frame the behaviour is enabled. Override this method to add custom enable logic.
        /// </summary>
        protected virtual void OnNetEnable(){ }

        /// <summary>
        /// Called the first frame the behaviour is disabled and on destroying it. Override this method to add custom disable logic.
        /// </summary>
        protected virtual void OnNetDisable(){ }

        /// <summary>
        /// Called once the object is spawned across the network. Enable/disable your NetBehaviours here, use it as any Awake event.
        /// </summary>
        protected virtual void OnNetSpawn(){ }

        /// <summary>
        /// Called when the owner of the behaviour is disconnected, handle here any owner transfer-ship.
        /// </summary>
        protected virtual void OnDisconnect(){}

        /// <summary>
        /// Sends an RPC call.
        /// </summary>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameters">The parameters to pass to the method.</param>
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

        /// <summary>
        /// Transfers ownership of this network object to a specific client.
        /// </summary>
        /// <param name="ownerId">The ID of the client that will own this object.</param>
        /// <param name="ownChildren">Whether to transfer ownership of child network objects as well.</param>
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
            enabled = false;                
            DebugQueue.AddMessage($"Disable {GetType().Name} | {gameObject.name}.", DebugQueue.MessageType.Warning);

            var behaviours = gameObject.GetComponents<NetBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour.NetObject != null)
                {
                    NetObject = behaviour.NetObject;
                    NetObject.Register(this);
                    return;
                }
            }
            NetScene.RegisterSceneObject(this);
        }
        internal void SetNetObject(NetObject obj)
        {
            if (obj == null) return;
            NetObject = obj;
            registered = true;
            enabled = false;
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
