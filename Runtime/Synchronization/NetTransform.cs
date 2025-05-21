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
        [SerializeField] private float _interpolationBackTime = 0.1f; // How far back to interpolate
        [SerializeField] private float _interpolationSpeed = 10f; // How fast to interpolate
        
        private struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public float Timestamp;
        }
        
        private Queue<TransformState> _stateBuffer = new Queue<TransformState>();
        private TransformState _targetState;
        private TransformState _currentState;
        private bool _hasTargetState;
        private Vector3 _lastPredictedPosition;
        private Quaternion _lastPredictedRotation;

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

            // Initialize states
            _currentState = new TransformState
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale,
                Timestamp = Time.time
            };
            _targetState = _currentState;
            _hasTargetState = true;

            if (isOwned)
            {
                StartPrediction();
            }
        }

        protected override void OnPredictionStart()
        {
            _lastPredictedPosition = transform.position;
            _lastPredictedRotation = transform.rotation;
        }

        protected override void OnStateReconcile()
        {
            // Store current predicted state
            var predictedPosition = transform.position;
            var predictedRotation = transform.rotation;

            // Apply authoritative state
            transform.position = new Vector3(_positionX, _positionY, _positionZ);
            transform.rotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);

            // Smoothly interpolate to predicted state if it's close enough
            if (Vector3.Distance(predictedPosition, transform.position) < 1f)
            {
                transform.position = Vector3.Lerp(transform.position, predictedPosition, 0.5f);
                transform.rotation = Quaternion.Slerp(transform.rotation, predictedRotation, 0.5f);
            }

            _lastPredictedPosition = transform.position;
            _lastPredictedRotation = transform.rotation;
        }

        private void Update()
        {
            if (!isOwned)
            {
                // Store new state when received
                if (_positionX != transform.position.x || _positionY != transform.position.y || _positionZ != transform.position.z ||
                    _rotationX != transform.rotation.x || _rotationY != transform.rotation.y || _rotationZ != transform.rotation.z || _rotationW != transform.rotation.w)
                {
                    var newState = new TransformState
                    {
                        Position = new Vector3(_positionX, _positionY, _positionZ),
                        Rotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW),
                        Scale = new Vector3(_scaleX, _scaleY, _scaleZ),
                        Timestamp = Time.time
                    };
                    
                    _stateBuffer.Enqueue(newState);
                    
                    // Remove old states
                    while (_stateBuffer.Count > 0 && _stateBuffer.Peek().Timestamp < Time.time - _interpolationBackTime)
                    {
                        _stateBuffer.Dequeue();
                    }
                    
                    // Update target state
                    if (_stateBuffer.Count > 0)
                    {
                        _targetState = _stateBuffer.Peek();
                        _hasTargetState = true;
                    }
                }

                // Interpolate to target state
                if (_hasTargetState)
                {
                    transform.position = Vector3.Lerp(transform.position, _targetState.Position, Time.deltaTime * _interpolationSpeed);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetState.Rotation, Time.deltaTime * _interpolationSpeed);
                    transform.localScale = Vector3.Lerp(transform.localScale, _targetState.Scale, Time.deltaTime * _interpolationSpeed);
                }
            }
            else
            {
                // For owned objects, update sync vars if not reconciling
                if (!_isPredicting)
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

        // Called when receiving authoritative state from server
        public void OnAuthoritativeStateReceived()
        {
            OnStateReceived();
        }
    }
}
