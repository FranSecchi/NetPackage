using UnityEngine;
using UnityEditor;
using NetPackage.Synchronization;

namespace NetPackage.Editor
{
    public class NetTransformEditor
    {
        [MenuItem("GameObject/NetPackage/NetTransform", false, 0)]
        public static void CreateNetTransform()
        {
            // Create a new GameObject
            GameObject go = new GameObject("NetTransform");
            
            // Add the NetTransform component
            go.AddComponent<NetTransform>();
            
            // Register the object in the scene
            Selection.activeGameObject = go;
            
            // Make sure the object is selected in the hierarchy
            EditorGUIUtility.PingObject(go);
        }
    }
} 