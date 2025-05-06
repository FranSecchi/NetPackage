using System;
using Runtime.NetPackage.Runtime.NetworkManager;
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
        
        // Start is called before the first frame update
        private void Awake()
        {
            if (!Application.isPlaying && NetObject == null)
            {
                Debug.Log("app");
                NetScene.Instance.RegisterSceneObject(this);
            }
        }


        public void SetNetObject(NetObject obj)
        {
            NetObject = obj;
            //OnNetObjectInitialized(); 
        }
        private void RegisterBehaviour()
        {
            if(NetObject != null)
                return;
            if (TryGetComponent(out NetBehaviour netBehaviour) && netBehaviour.NetObject != null)
            {
                NetObject.Register(this);
            }
            else NetScene.Instance.RegisterSceneObject(this);
        }
    }
}
