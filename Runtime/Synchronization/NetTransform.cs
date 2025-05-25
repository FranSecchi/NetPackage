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
            if (_syncPosition)
            {
                bool shouldUpdatePosition = true;
                if (changes.ContainsKey("_positionX")) 
                {
                    float newX = (float)changes["_positionX"];
                    shouldUpdatePosition &= Mathf.Abs(newX - transform.position.x) <= _positionThreshold;
                }
                if (changes.ContainsKey("_positionY")) 
                {
                    float newY = (float)changes["_positionY"];
                    shouldUpdatePosition &= Mathf.Abs(newY - transform.position.y) <= _positionThreshold;
                }
                if (changes.ContainsKey("_positionZ")) 
                {
                    float newZ = (float)changes["_positionZ"];
                    shouldUpdatePosition &= Mathf.Abs(newZ - transform.position.z) <= _positionThreshold;
                }

                if (shouldUpdatePosition)
                {
                    if (changes.ContainsKey("_positionX")) _positionX = (float)changes["_positionX"];
                    if (changes.ContainsKey("_positionY")) _positionY = (float)changes["_positionY"];
                    if (changes.ContainsKey("_positionZ")) _positionZ = (float)changes["_positionZ"];
                    _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
                }
            }

            if (_syncRotation)
            {
                bool shouldUpdateRotation = true;
                if (changes.ContainsKey("_rotationX")) 
                {
                    float newX = (float)changes["_rotationX"];
                    shouldUpdateRotation &= Mathf.Abs(newX - transform.rotation.x) <= _rotationThreshold;
                }
                if (changes.ContainsKey("_rotationY")) 
                {
                    float newY = (float)changes["_rotationY"];
                    shouldUpdateRotation &= Mathf.Abs(newY - transform.rotation.y) <= _rotationThreshold;
                }
                if (changes.ContainsKey("_rotationZ")) 
                {
                    float newZ = (float)changes["_rotationZ"];
                    shouldUpdateRotation &= Mathf.Abs(newZ - transform.rotation.z) <= _rotationThreshold;
                }
                if (changes.ContainsKey("_rotationW")) 
                {
                    float newW = (float)changes["_rotationW"];
                    shouldUpdateRotation &= Mathf.Abs(newW - transform.rotation.w) <= _rotationThreshold;
                }

                if (shouldUpdateRotation)
                {
                    if (changes.ContainsKey("_rotationX")) _rotationX = (float)changes["_rotationX"];
                    if (changes.ContainsKey("_rotationY")) _rotationY = (float)changes["_rotationY"];
                    if (changes.ContainsKey("_rotationZ")) _rotationZ = (float)changes["_rotationZ"];
                    if (changes.ContainsKey("_rotationW")) _rotationW = (float)changes["_rotationW"];
                    _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
                }
            }

            if (_syncScale)
            {
                bool shouldUpdateScale = true;
                if (changes.ContainsKey("_scaleX")) 
                {
                    float newX = (float)changes["_scaleX"];
                    shouldUpdateScale &= Mathf.Abs(newX - transform.localScale.x) <= _scaleThreshold;
                }
                if (changes.ContainsKey("_scaleY")) 
                {
                    float newY = (float)changes["_scaleY"];
                    shouldUpdateScale &= Mathf.Abs(newY - transform.localScale.y) <= _scaleThreshold;
                }
                if (changes.ContainsKey("_scaleZ")) 
                {
                    float newZ = (float)changes["_scaleZ"];
                    shouldUpdateScale &= Mathf.Abs(newZ - transform.localScale.z) <= _scaleThreshold;
                }

                if (shouldUpdateScale)
                {
                    if (changes.ContainsKey("_scaleX")) _scaleX = (float)changes["_scaleX"];
                    if (changes.ContainsKey("_scaleY")) _scaleY = (float)changes["_scaleY"];
                    if (changes.ContainsKey("_scaleZ")) _scaleZ = (float)changes["_scaleZ"];
                    _targetScale = new Vector3(_scaleX, _scaleY, _scaleZ);
                }
            }
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
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
                _positionX = transform.position.x + transform.position.x - lastState.GetFieldValue<float>(this,"_positionX");
                _positionY = transform.position.y + transform.position.y - lastState.GetFieldValue<float>(this,"_positionY");
                _positionZ = transform.position.z + transform.position.z - lastState.GetFieldValue<float>(this,"_positionZ");
                _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
            }

            if (_syncRotation)
            {
                _rotationX = transform.rotation.x + transform.rotation.x - lastState.GetFieldValue<float>(this,"_rotationX");
                _rotationY = transform.rotation.y + transform.rotation.y - lastState.GetFieldValue<float>(this,"_rotationY");
                _rotationZ = transform.rotation.z + transform.rotation.z - lastState.GetFieldValue<float>(this,"_rotationZ");
                _rotationW = transform.rotation.w + transform.rotation.w - lastState.GetFieldValue<float>(this,"_rotationW");
                _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
            }

            if (_syncScale)
            {
                _scaleX = transform.localScale.x + transform.localScale.x - lastState.GetFieldValue<float>(this,"_scaleX");
                _scaleY = transform.localScale.y + transform.localScale.y - lastState.GetFieldValue<float>(this,"_scaleY");
                _scaleZ = transform.localScale.z + transform.localScale.z - lastState.GetFieldValue<float>(this,"_scaleZ");
                _targetScale = new Vector3(_scaleX, _scaleY, _scaleZ);
            }
        }


        protected override void OnPausePrediction()
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
            _isPredicting = false;
        }

        protected override void OnResumePrediction()
        {
            _isPredicting = true;
        }
    }
}
