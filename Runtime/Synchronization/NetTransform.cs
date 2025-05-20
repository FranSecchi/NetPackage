using System;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public class NetTransform : NetBehaviour
    {
        [Sync] private Vector3 _postition;
        [Sync] private Quaternion _rotation;
        [Sync] private Vector3 _scale;

        private void Update()
        {
            _postition = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
        }

        private void LateUpdate()
        {
            transform.position = _postition;
            transform.rotation = _rotation;
            transform.localScale = _scale;
        }
    }
}
