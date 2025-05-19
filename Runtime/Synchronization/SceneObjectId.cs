using System;
using UnityEditor;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [ExecuteAlways]
    public class SceneObjectId : MonoBehaviour
    {
        public string sceneId;

        
        void Awake()
        {
            // if (string.IsNullOrEmpty(sceneId))
            // {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    sceneId = Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(this);
                    Debug.Log($"Generated new ID for {gameObject.name}: {sceneId}");
                }
#endif
            // }
        }
    }
}
