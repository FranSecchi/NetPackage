using System;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [ExecuteAlways]
    public class SceneObjectId : MonoBehaviour
    {
        [SerializeField]
        private long _sceneId;
        public long sceneId => _sceneId;

        private void Awake()
        {
            if (_sceneId == 0)
                _sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (_sceneId == 0)
                _sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        }

        private void OnValidate()
        {
            if (_sceneId == 0)
                _sceneId = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        }
#endif
    }
}
