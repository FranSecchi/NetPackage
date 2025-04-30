using System;
using Runtime.NetPackage.Runtime.NetworkManager;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    public abstract class NetBehaviour : MonoBehaviour
    {
        public NetObject NetObject;
        public bool isOwned = true;
        protected bool spawned;
        
        // Start is called before the first frame update
        private void Awake()
        {
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        private void Update()
        {
        
        }

        public void OnSpawn()
        {
            spawned = true;
            NetSpawn();
        }
        public virtual void NetSpawn(){}

        public void PreAwakeInitialize()
        {
            if(NetObject != null)
                return;
            if (TryGetComponent(out NetBehaviour netBehaviour))
            {
                NetObject = netBehaviour.NetObject;
                NetObject.Register(this);
            }
            else NetObject = new NetObject(this);
        }
    }
}
