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
            GameObject go = new GameObject("NetTransform");
            go.AddComponent<NetTransform>();
            
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
        [MenuItem("GameObject/NetPackage/NetRigidbody", false, 1)]
        public static void CreateNetRigidBody()
        {
            GameObject go = new GameObject("NetRigidBody");
            go.AddComponent<NetRigidBody>();
            
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
        [MenuItem("GameObject/NetPackage/NetRigidBody2D", false, 2)]
        public static void CreateNetRigidbody2D()
        {
            GameObject go = new GameObject("NetRigidBody2D");
            go.AddComponent<NetRigidBody2D>();
            
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
} 