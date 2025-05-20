using System;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public class NetTransform : NetBehaviour
    {
        [Sync] private Vector3 _postition;
        [Sync] private float _rotationX;
        [Sync] private float _rotationY;
        [Sync] private float _rotationZ;
        [Sync] private float _rotationW;
        [Sync] private Vector3 _scale;

        private void Update()
        {
            _postition = transform.position;
            _rotationX = transform.rotation.x;
            _rotationY = transform.rotation.y;
            _rotationZ = transform.rotation.z;
            _rotationW = transform.rotation.w;
            _scale = transform.localScale;
        }

        private void LateUpdate()
        {
            transform.position = _postition;
            transform.rotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
            transform.localScale = _scale;
        }
    }
}
