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
        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private bool _isSynchronized = true;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;
        private bool _hasTargetState;

        public void Reset()
        {
            _isSynchronized = false;
            _hasTargetState = false;
            
            // Update the sync variables to match current transform
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

        public void ResumeSynchronization()
        {
            _isSynchronized = true;
        }

        protected override void OnNetSpawn()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;

            // Initialize sync variables
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

            // Initialize target state
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
            _hasTargetState = true;
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            if (changes.ContainsKey("_positionX")) _positionX = (float)changes["_positionX"];
            if (changes.ContainsKey("_positionY")) _positionY = (float)changes["_positionY"];
            if (changes.ContainsKey("_positionZ")) _positionZ = (float)changes["_positionZ"];
            if (changes.ContainsKey("_rotationX")) _rotationX = (float)changes["_rotationX"];
            if (changes.ContainsKey("_rotationY")) _rotationY = (float)changes["_rotationY"];
            if (changes.ContainsKey("_rotationZ")) _rotationZ = (float)changes["_rotationZ"];
            if (changes.ContainsKey("_rotationW")) _rotationW = (float)changes["_rotationW"];
            if (changes.ContainsKey("_scaleX")) _scaleX = (float)changes["_scaleX"];
            if (changes.ContainsKey("_scaleY")) _scaleY = (float)changes["_scaleY"];
            if (changes.ContainsKey("_scaleZ")) _scaleZ = (float)changes["_scaleZ"];

            // Update target state
            _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
            _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
            _targetScale = new Vector3(_scaleX, _scaleY, _scaleZ);
            _hasTargetState = true;
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            float positionThreshold = _desyncThreshold;
            float rotationThreshold = _desyncThreshold;
            float scaleThreshold = _desyncThreshold;

            if (changes.ContainsKey("_positionX") && Mathf.Abs((float)changes["_positionX"] - transform.position.x) > positionThreshold) return true;
            if (changes.ContainsKey("_positionY") && Mathf.Abs((float)changes["_positionY"] - transform.position.y) > positionThreshold) return true;
            if (changes.ContainsKey("_positionZ") && Mathf.Abs((float)changes["_positionZ"] - transform.position.z) > positionThreshold) return true;
            
            if (changes.ContainsKey("_rotationX") && Mathf.Abs((float)changes["_rotationX"] - transform.rotation.x) > rotationThreshold) return true;
            if (changes.ContainsKey("_rotationY") && Mathf.Abs((float)changes["_rotationY"] - transform.rotation.y) > rotationThreshold) return true;
            if (changes.ContainsKey("_rotationZ") && Mathf.Abs((float)changes["_rotationZ"] - transform.rotation.z) > rotationThreshold) return true;
            if (changes.ContainsKey("_rotationW") && Mathf.Abs((float)changes["_rotationW"] - transform.rotation.w) > rotationThreshold) return true;
            
            if (changes.ContainsKey("_scaleX") && Mathf.Abs((float)changes["_scaleX"] - transform.localScale.x) > scaleThreshold) return true;
            if (changes.ContainsKey("_scaleY") && Mathf.Abs((float)changes["_scaleY"] - transform.localScale.y) > scaleThreshold) return true;
            if (changes.ContainsKey("_scaleZ") && Mathf.Abs((float)changes["_scaleZ"] - transform.localScale.z) > scaleThreshold) return true;

            return false;
        }

        protected override void Predict(float deltaTime)
        {
            // For transform, prediction is handled by the physics system
            // We just need to update our sync variables
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
            if (!NetManager.Active || !NetManager.Running || !_isSynchronized)
                return;

            if (!isOwned)
            {
                // Get state from NetObject
                if (NetObject?.State != null)
                {
                    _positionX = GetFieldValue<float>("_positionX");
                    _positionY = GetFieldValue<float>("_positionY");
                    _positionZ = GetFieldValue<float>("_positionZ");
                    _rotationX = GetFieldValue<float>("_rotationX");
                    _rotationY = GetFieldValue<float>("_rotationY");
                    _rotationZ = GetFieldValue<float>("_rotationZ");
                    _rotationW = GetFieldValue<float>("_rotationW");
                    _scaleX = GetFieldValue<float>("_scaleX");
                    _scaleY = GetFieldValue<float>("_scaleY");
                    _scaleZ = GetFieldValue<float>("_scaleZ");

                    // Update target state
                    _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
                    _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
                    _targetScale = new Vector3(_scaleX, _scaleY, _scaleZ);
                    _hasTargetState = true;
                }

                // Interpolate to target state
                if (_hasTargetState)
                {
                    transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _interpolationSpeed);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _interpolationSpeed);
                    transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _interpolationSpeed);
                }
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
