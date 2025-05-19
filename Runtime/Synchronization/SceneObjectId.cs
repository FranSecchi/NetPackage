using System;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [ExecuteAlways]
    public class SceneObjectId : MonoBehaviour
    {
        public long sceneId;


        private void Awake()
        {
            if (sceneId == 0)
            {
                sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sceneId == 0)
            {
                sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
            }
        }
#endif
    }
}
