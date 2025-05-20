using System;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public class NetTransform : NetBehaviour
    {
        [Sync] private float _positionX;
        [Sync] private float _positionY;
        [Sync] private float _positionZ;
        [Sync] private float _rotationX;
        [Sync] private float _rotationY;
        [Sync] private float _rotationZ;
        [Sync] private float _rotationW;
        [Sync] private float _scaleX;
        [Sync] private float _scaleY;
        [Sync] private float _scaleZ;

        protected override void OnNetSpawn()
        {
            _positionX = transform.position.x;
            _positionY = transform.position.y;
            _positionZ = transform.position.z;
            _rotationX = transform.rotation.x;
            _rotationY = transform.rotation.y;
            _rotationZ = transform.rotation.z;
            _rotationW = transform.rotation.w;
            _scaleX = transform.localScale.x;
            _scaleY = transform.localScale.y;
            _scaleZ = transform.localScale.z;
        }

        private void Update()
        {
            if (!isOwned)
            {
                transform.position = new Vector3(_positionX, _positionY, _positionZ);
                transform.rotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
                transform.localScale = new Vector3(_scaleX, _scaleY, _scaleZ);
            }
            else
            {
                _positionX = transform.position.x;
                _positionY = transform.position.y;
                _positionZ = transform.position.z;
                _rotationX = transform.rotation.x;
                _rotationY = transform.rotation.y;
                _rotationZ = transform.rotation.z;
                _rotationW = transform.rotation.w;
                _scaleX = transform.localScale.x;
                _scaleY = transform.localScale.y;
                _scaleZ = transform.localScale.z;
            }
        }
    }
}
