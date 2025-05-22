using System;
using System.Collections.Generic;
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

        // Interpolation settings
        [SerializeField] private float _interpolationSpeed = 10f; // How fast to interpolate
        [SerializeField] private float _positionTolerance = 0.1f; // How much position difference is allowed
        [SerializeField] private float _rotationTolerance = 5f; // How much rotation difference is allowed (in degrees)

        protected override void OnNetSpawn()
        {
            // Initialize sync vars with current transform values
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

            if (isOwned)
            {
                StartPrediction();
            }
        }

        protected override void Predict(float deltaTime)
        {
            if (isOwned)
            {
                // Update sync vars with current transform values
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

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            // Check position changes
            if (HasPositionChanges(changes, out Vector3 newPos))
            {
                return Vector3.Distance(transform.position, newPos) > _positionTolerance;
            }

            // Check rotation changes
            if (HasRotationChanges(changes, out Quaternion newRot))
            {
                return Quaternion.Angle(transform.rotation, newRot) > _rotationTolerance;
            }

            return false;
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            // Handle position changes
            if (HasPositionChanges(changes, out Vector3 newPos))
            {
                if (isOwned)
                {
                    transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * _interpolationSpeed);
                }
                else
                {
                    transform.position = newPos;
                }
            }

            // Handle rotation changes
            if (HasRotationChanges(changes, out Quaternion newRot))
            {
                if (isOwned)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRot, Time.deltaTime * _interpolationSpeed);
                }
                else
                {
                    transform.rotation = newRot;
                }
            }

            // Handle scale changes
            if (HasScaleChanges(changes, out Vector3 newScale))
            {
                if (isOwned)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, newScale, Time.deltaTime * _interpolationSpeed);
                }
                else
                {
                    transform.localScale = newScale;
                }
            }
        }

        private bool HasPositionChanges(Dictionary<string, object> changes, out Vector3 newPos)
        {
            newPos = transform.position;
            bool hasChanges = false;

            if (changes.TryGetValue("_positionX", out object x)) { newPos.x = (float)x; hasChanges = true; }
            if (changes.TryGetValue("_positionY", out object y)) { newPos.y = (float)y; hasChanges = true; }
            if (changes.TryGetValue("_positionZ", out object z)) { newPos.z = (float)z; hasChanges = true; }

            return hasChanges;
        }

        private bool HasRotationChanges(Dictionary<string, object> changes, out Quaternion newRot)
        {
            newRot = transform.rotation;
            bool hasChanges = false;

            if (changes.TryGetValue("_rotationX", out object x)) { newRot.x = (float)x; hasChanges = true; }
            if (changes.TryGetValue("_rotationY", out object y)) { newRot.y = (float)y; hasChanges = true; }
            if (changes.TryGetValue("_rotationZ", out object z)) { newRot.z = (float)z; hasChanges = true; }
            if (changes.TryGetValue("_rotationW", out object w)) { newRot.w = (float)w; hasChanges = true; }

            return hasChanges;
        }

        private bool HasScaleChanges(Dictionary<string, object> changes, out Vector3 newScale)
        {
            newScale = transform.localScale;
            bool hasChanges = false;

            if (changes.TryGetValue("_scaleX", out object x)) { newScale.x = (float)x; hasChanges = true; }
            if (changes.TryGetValue("_scaleY", out object y)) { newScale.y = (float)y; hasChanges = true; }
            if (changes.TryGetValue("_scaleZ", out object z)) { newScale.z = (float)z; hasChanges = true; }

            return hasChanges;
        }
    }
}
