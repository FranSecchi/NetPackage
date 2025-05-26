using System.Collections.Generic;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [CreateAssetMenu(fileName = "NetPrefabs", menuName = "ScriptableObjects/PrefabList", order = 1)]
    public class NetPrefabRegistry : ScriptableObject
    {
        /// <summary>
        /// List of networked GameObjects that will be instantiated at runtime
        /// </summary>
        public List<GameObject> prefabs = new List<GameObject>();
    }
}
