using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using SimpleNet.Messages;
using SimpleNet.Network;
using SimpleNet.Transport;
using SimpleNet.Transport.UDP;
using SimpleNet.Utilities;

namespace SimpleNet.Synchronization
{
    [ExecuteAlways]
    internal class SceneObjectId : MonoBehaviour
    {
        [SerializeField] private string sceneId;
        public string SceneId => sceneId;

        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Run in edit mode only
            if (!Application.isPlaying)
            {
                // If no ID yet or it's a duplicate in the scene, generate a new one
                if (string.IsNullOrEmpty(sceneId) || HasDuplicateIdInScene())
                {
                    sceneId = Guid.NewGuid().ToString();
                    EditorUtility.SetDirty(this);
                }
            }
        }

        private bool HasDuplicateIdInScene()
        {
            var allObjects = FindObjectsOfType<SceneObjectId>();
            foreach (var obj in allObjects)
            {
                if (obj != this && obj.sceneId == this.sceneId)
                    return true;
            }
            return false;
        }
#endif
    }
}
