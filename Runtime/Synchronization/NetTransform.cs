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

        [Header("Desync Thresholds")]
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 0.01f;
        [SerializeField] private float _scaleThreshold = 0.01f;

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

        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;

        [SerializeField] public float syncPrecision = 0.01f; // Default to 1cm precision

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

        protected override void OnNetSpawn()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
            if (!_syncPosition && !_syncRotation && !_syncScale)
            {
                enabled = false;
                return;
            }
            PausePrediction();
            ResumePrediction();
        }

        private void Update()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;

            
            if (!isOwned)
            {
                if (_syncPosition)
                    _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
    
                if (_syncRotation)
                    _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
    
                if (_syncScale)
                    _targetScale = new Vector3(_scaleX, _scaleY, _scaleZ);
            }
            float lerpSpeed = Time.deltaTime * _interpolationSpeed;

            if (_syncPosition)
                transform.position = Vector3.Lerp(transform.position, _targetPosition, lerpSpeed);

            if (_syncRotation)
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, lerpSpeed);

            if (_syncScale)
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, lerpSpeed);
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            if (!isOwned) return;
            
            if (_syncPosition)
            {
                if (changes.ContainsKey("_positionX")) _targetPosition.x = (float)changes["_positionX"];
                if (changes.ContainsKey("_positionY")) _targetPosition.y = (float)changes["_positionY"];
                if (changes.ContainsKey("_positionZ")) _targetPosition.z = (float)changes["_positionZ"];
            }
            
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX")) _targetRotation.x = (float)changes["_rotationX"];
                if (changes.ContainsKey("_rotationY")) _targetRotation.y = (float)changes["_rotationY"];
                if (changes.ContainsKey("_rotationZ")) _targetRotation.z = (float)changes["_rotationZ"];
                if (changes.ContainsKey("_rotationW")) _targetRotation.w = (float)changes["_rotationW"];
            }
            
            if (_syncScale)
            {
                if (changes.ContainsKey("_scaleX")) _targetScale.x = (float)changes["_scaleX"];
                if (changes.ContainsKey("_scaleY")) _targetScale.y = (float)changes["_scaleY"];
                if (changes.ContainsKey("_scaleZ")) _targetScale.z = (float)changes["_scaleZ"];
            }
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            if (_syncPosition)
            {
                if (changes.ContainsKey("_positionX") && Mathf.Abs((float)changes["_positionX"] - transform.position.x) > _positionThreshold) return true;
                if (changes.ContainsKey("_positionY") && Mathf.Abs((float)changes["_positionY"] - transform.position.y) > _positionThreshold) return true;
                if (changes.ContainsKey("_positionZ") && Mathf.Abs((float)changes["_positionZ"] - transform.position.z) > _positionThreshold) return true;
            }
            
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX") && Mathf.Abs((float)changes["_rotationX"] - transform.rotation.x) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationY") && Mathf.Abs((float)changes["_rotationY"] - transform.rotation.y) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationZ") && Mathf.Abs((float)changes["_rotationZ"] - transform.rotation.z) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationW") && Mathf.Abs((float)changes["_rotationW"] - transform.rotation.w) > _rotationThreshold) return true;
            }
            
            if (_syncScale)
            {
                if (changes.ContainsKey("_scaleX") && Mathf.Abs((float)changes["_scaleX"] - transform.localScale.x) > _scaleThreshold) return true;
                if (changes.ContainsKey("_scaleY") && Mathf.Abs((float)changes["_scaleY"] - transform.localScale.y) > _scaleThreshold) return true;
                if (changes.ContainsKey("_scaleZ") && Mathf.Abs((float)changes["_scaleZ"] - transform.localScale.z) > _scaleThreshold) return true;
            }

            return false;
        }
        
        protected override void Predict(float deltaTime, ObjectState lastState, List<RollbackManager.InputCommand> lastInputs)
        {
            if (_syncPosition)
            {
                _positionY = Quantize(transform.position.y);
                _positionX = Quantize(transform.position.x);
                _positionZ = Quantize(transform.position.z);
                _targetPosition = transform.position;
            }

            if (_syncRotation)
            {
                _rotationX = Quantize(transform.rotation.x);
                _rotationY = Quantize(transform.rotation.y);
                _rotationZ = Quantize(transform.rotation.z);
                _rotationW = Quantize(transform.rotation.w);
                _targetRotation = transform.rotation;
            }

            if (_syncScale)
            {
                _scaleX = Quantize(transform.localScale.x);
                _scaleY = Quantize(transform.localScale.y);
                _scaleZ = Quantize(transform.localScale.z);
                _targetScale = transform.localScale;
            }
        }

        protected override void OnPausePrediction()
        {
            SetState();
        }

        protected override void OnResumePrediction()
        {
        }

        private float Quantize(float value)
        {
            return Mathf.Round(value / syncPrecision) * syncPrecision;
        }

        private void SetState()
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
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            _targetScale = transform.localScale;
        }
    }
}
