using System;
using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public class NetTransform : NetBehaviour
    {
        [Header("Synchronization Settings")]
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncScale = true;

        [Sync] private Vector3 _position;
        [Sync] private Quaternion _rotation;
        [Sync] private Vector3 _scale;

        // Interpolation settings
        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private bool _isSynchronized = true;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;
        private bool _hasTargetState;

        public bool SyncPosition
        {
            get => _syncPosition;
            set => _syncPosition = value;
        }

        public bool SyncRotation
        {
            get => _syncRotation;
            set => _syncRotation = value;
        }

        public bool SyncScale
        {
            get => _syncScale;
            set => _syncScale = value;
        }

        public void Reset()
        {
            _isSynchronized = false;
            _hasTargetState = false;
            
            // Update the sync variables to match current transform
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
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
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;

            // Initialize target state
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
            _hasTargetState = true;
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            if (_syncPosition && changes.ContainsKey("_position"))
            {
                _position = (Vector3)changes["_position"];
            }

            if (_syncRotation && changes.ContainsKey("_rotation"))
            {
                _rotation = (Quaternion)changes["_rotation"];
            }

            if (_syncScale && changes.ContainsKey("_scale"))
            {
                _scale = (Vector3)changes["_scale"];
            }

            // Update target state
            _targetPosition = _position;
            _targetRotation = _rotation;
            _targetScale = _scale;
            _hasTargetState = true;
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            float positionThreshold = _desyncThreshold;
            float rotationThreshold = _desyncThreshold;
            float scaleThreshold = _desyncThreshold;

            if (_syncPosition && changes.ContainsKey("_position"))
            {
                Vector3 newPosition = (Vector3)changes["_position"];
                if (Vector3.Distance(transform.position, newPosition) > positionThreshold) return true;
            }
            
            if (_syncRotation && changes.ContainsKey("_rotation"))
            {
                Quaternion newRotation = (Quaternion)changes["_rotation"];
                if (Quaternion.Angle(transform.rotation, newRotation) > rotationThreshold) return true;
            }
            
            if (_syncScale && changes.ContainsKey("_scale"))
            {
                Vector3 newScale = (Vector3)changes["_scale"];
                if (Vector3.Distance(transform.localScale, newScale) > scaleThreshold) return true;
            }

            return false;
        }

        protected override void Predict(float deltaTime)
        {
            // For transform, prediction is handled by the physics system
            // We just need to update our sync variables
            if (_syncPosition)
            {
                _position = transform.position;
            }

            if (_syncRotation)
            {
                _rotation = transform.rotation;
            }

            if (_syncScale)
            {
                _scale = transform.localScale;
            }
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
                    if (_syncPosition)
                    {
                        _position = GetFieldValue<Vector3>("_position");
                    }

                    if (_syncRotation)
                    {
                        _rotation = GetFieldValue<Quaternion>("_rotation");
                    }

                    if (_syncScale)
                    {
                        _scale = GetFieldValue<Vector3>("_scale");
                    }

                    // Update target state
                    _targetPosition = _position;
                    _targetRotation = _rotation;
                    _targetScale = _scale;
                    _hasTargetState = true;
                }

                // Interpolate to target state
                if (_hasTargetState)
                {
                    if (_syncPosition)
                        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _interpolationSpeed);
                    if (_syncRotation)
                        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _interpolationSpeed);
                    if (_syncScale)
                        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _interpolationSpeed);
                }
            }
            else
            {
                if (_syncPosition)
                {
                    _position = transform.position;
                }

                if (_syncRotation)
                {
                    _rotation = transform.rotation;
                }

                if (_syncScale)
                {
                    _scale = transform.localScale;
                }
            }
        }
    }
}
