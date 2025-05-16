using System.Collections.Generic;
using UnityEngine;

namespace NetPackage.Runtime.Synchronization
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PrefabList", order = 1)]
    public class NetPrefabRegistry : ScriptableObject
    {
        public List<GameObject> prefabs = new List<GameObject>();
    }
}
