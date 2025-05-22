using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetPackage.Synchronization
{
    [ExecuteAlways]
    public class SceneObjectId : MonoBehaviour
    {
        public string sceneId;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            var scene = gameObject.scene;

            // Only assign if scene is valid, loaded state is false (scene is still being loaded),
            // and ID is not yet assigned
            if (scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(sceneId))
            {
                sceneId = "";
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            // In the editor, don't assign unless it's missing and we're in edit mode
            if (!Application.isPlaying)
            {
                if (string.IsNullOrEmpty(sceneId) || HasDuplicateIdInScene())
                {
                    sceneId = Guid.NewGuid().ToString();
                    UnityEditor.EditorUtility.SetDirty(this);
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