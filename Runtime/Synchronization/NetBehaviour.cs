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
        public bool isOwned = true;
        protected bool spawned;
        public bool registered = false;
        
        private void Awake()
        {
            RegisterAsSceneObject();
        }

        private void OnEnable()
        {
            StateManager.Register(NetObject.NetId, this);
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
