using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PrefabList", order = 1)]
    public class NetPrefabRegistry : ScriptableObject
    {
        public List<GameObject> prefabs = new List<GameObject>();
    }
}
