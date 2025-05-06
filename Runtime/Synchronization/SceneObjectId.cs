using System;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    [ExecuteAlways]
    public class SceneObjectId : MonoBehaviour
    {
        [NonSerialized]
        public long sceneId;

#if UNITY_EDITOR
        private void Reset()
        {
            if (sceneId == 0)
                sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        }
#endif
    }
}
