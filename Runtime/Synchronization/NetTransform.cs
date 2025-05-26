using System;
using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public class NetTransform : NetBehaviour
    {
        [Header("Synchronizes")]
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncScale = true;
        [SerializeField] public float syncPrecision = 0.01f; 

        [Header("Desync Thresholds")]
        [Tooltip("For rollback")]
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 0.01f;
        [SerializeField] private float _scaleThreshold = 0.01f;

        [Header("Interpolation Settings")]
        [Space(5)]
        [Tooltip("These apply only on non-owned objects")]
        [SerializeField] private float _interpolationSpeed = 10f;
        [SerializeField] private AnimationCurve _interpolationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private float _maxTeleportDistance = 10f;
        [SerializeField] private bool _useVelocityBasedInterpolation = false;
        [Tooltip("Smooth")]
        [SerializeField] private float _positionSmoothingFactor = 1f;
        [SerializeField] private float _rotationSmoothingFactor = 1f;
        [SerializeField] private float _scaleSmoothingFactor = 1f;

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

        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;



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
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastScale = transform.localScale;
            SetState();
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


                float lerpSpeed = Time.deltaTime * _interpolationSpeed;
                float curveValue = _interpolationCurve.Evaluate(lerpSpeed);

                if (_syncPosition)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Vector3 velocity = (_targetPosition - _lastPosition) / Time.deltaTime;
                        _targetPosition += velocity * Time.deltaTime;
                    }
                    if (Vector3.Distance(transform.position, _targetPosition) > _maxTeleportDistance)
                    {
                        transform.position = _targetPosition;
                    }
                    else
                    {
                        transform.position = Vector3.Lerp(transform.position, _targetPosition, curveValue * _positionSmoothingFactor);
                    }
                }

                if (_syncRotation)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Quaternion deltaRotation = Quaternion.Inverse(_lastRotation) * _targetRotation;
                        _targetRotation = _lastRotation * Quaternion.Slerp(Quaternion.identity, deltaRotation, Time.deltaTime);
                    }

                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, curveValue * _rotationSmoothingFactor);
                }

                if (_syncScale)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Vector3 scaleVelocity = (_targetScale - _lastScale) / Time.deltaTime;
                        _targetScale += scaleVelocity * Time.deltaTime;
                    }

                    transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, curveValue * _scaleSmoothingFactor);
                }

                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
                _lastScale = transform.localScale;
            }
            else SetState();
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
        
        protected override void OnPausePrediction()
        {
            SetState();
        }
        protected override void Predict(float deltaTime, ObjectState lastState, List<RollbackManager.InputCommand> lastInputs)
        {
            if (_syncPosition)
            {
                _targetPosition = transform.position;
            }

            if (_syncRotation)
            {
                _targetRotation = transform.rotation;
            }

            if (_syncScale)
            {
                _targetScale = transform.localScale;
            }
        }


        private void SetState()
        {
            if (_syncPosition)
            {
                _positionY = Quantize(transform.position.y);
                _positionX = Quantize(transform.position.x);
                _positionZ = Quantize(transform.position.z);
            
            }
            if (_syncRotation)
            {
                _rotationX = Quantize(transform.rotation.x);
                _rotationY = Quantize(transform.rotation.y);
                _rotationZ = Quantize(transform.rotation.z);
                _rotationW = Quantize(transform.rotation.w);
            }
            if (_syncScale)
            {
                _scaleX = Quantize(transform.localScale.x);
                _scaleY = Quantize(transform.localScale.y);
                _scaleZ = Quantize(transform.localScale.z);
            }
        }
        private float Quantize(float value)
        {
            return Mathf.Round(value / syncPrecision) * syncPrecision;
        }

    }
}
