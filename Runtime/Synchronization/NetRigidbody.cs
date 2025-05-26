using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetRigidbody : NetBehaviour
    {
        [Header("Synchronization Settings")]
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncVelocity = true;
        [SerializeField] private bool _syncProperties = true;
        [SerializeField] public float syncPrecision = 0.01f; 

        [Header("Desync Thresholds")]
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 0.01f;
        [SerializeField] private float _velocityThreshold = 0.1f;

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
        [SerializeField] private float _velocitySmoothingFactor = 1f;

        [Header("Physics Settings")]
        [SerializeField] private bool _predictPhysics = true;
        [SerializeField] private float _predictionTime = 0.1f;
        [SerializeField] private int _predictionSteps = 3;
        [SerializeField] private bool _smoothAngularVelocity = true;
        [SerializeField] private float _angularVelocitySmoothing = 0.1f;

        [Sync] private float _positionX;
        [Sync] private float _positionY;
        [Sync] private float _positionZ;
        [Sync] private float _rotationX;
        [Sync] private float _rotationY;
        [Sync] private float _rotationZ;
        [Sync] private float _rotationW;
        [Sync] private float _velocityX;
        [Sync] private float _velocityY;
        [Sync] private float _velocityZ;
        [Sync] private float _angularVelocityX;
        [Sync] private float _angularVelocityY;
        [Sync] private float _angularVelocityZ;
        
        [Sync] private float _mass;
        [Sync] private float _drag;
        [Sync] private float _angularDrag;
        [Sync] private bool _useGravity;

        private Rigidbody _rigidbody;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetVelocity;
        private Vector3 _targetAngularVelocity;
        private List<ForceCommand> _pendingForces = new List<ForceCommand>();
        private bool _wasKinematic;
        private CollisionDetectionMode _wasCollisionDetection;
        private RigidbodyInterpolation _wasInterpolation;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastVelocity;
        private Vector3 _lastAngularVelocity;

        private struct ForceCommand
        {
            public Vector3 Force;
            public ForceMode Mode;
            public float DeltaTime;
        }

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

        public bool SyncVelocity
        {
            get => _syncVelocity;
            set => _syncVelocity = value;
        }

        public bool SyncProperties
        {
            get => _syncProperties;
            set => _syncProperties = value;
        }

        protected override void OnNetSpawn()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
            _rigidbody = GetComponent<Rigidbody>();

            if(!isOwned)
            {
                _wasKinematic = _rigidbody.isKinematic;
                _wasCollisionDetection = _rigidbody.collisionDetectionMode;
                _wasInterpolation = _rigidbody.interpolation;
                
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.isKinematic = true;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                
                _syncProperties = true;
            }

            _lastPosition = _rigidbody.position;
            _lastRotation = _rigidbody.rotation;
            _lastVelocity = _rigidbody.velocity;
            _lastAngularVelocity = _rigidbody.angularVelocity;

            SetState();

            _targetPosition = _rigidbody.position;
            _targetRotation = _rigidbody.rotation;
            _targetVelocity = _rigidbody.velocity;
            _targetAngularVelocity = _rigidbody.angularVelocity;
        }

        protected override void OnNetEnable()
        {
            if(!isOwned)
            {
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                _rigidbody.isKinematic = true;
                _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                
                _syncProperties = true;
            }
        }

        protected override void OnNetDisable()
        {
            base.OnNetDisable();
            
            if (!isOwned && _rigidbody != null)
            {
                _rigidbody.isKinematic = _wasKinematic;
                _rigidbody.collisionDetectionMode = _wasCollisionDetection;
                _rigidbody.interpolation = _wasInterpolation;
            }
        }

        /// <summary>
        /// Adds a force to be applied during prediction. This force will be synchronized across the network.
        /// </summary>
        /// <param name="force">The force to apply</param>
        /// <param name="mode">The mode in which to apply the force</param>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            if (!isOwned) return;

            _pendingForces.Add(new ForceCommand
            {
                Force = force,
                Mode = mode,
                DeltaTime = Time.fixedDeltaTime
            });

            _rigidbody.AddForce(force, mode);
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
            if (_syncVelocity)
            {
                if (changes.ContainsKey("_velocityX")) _targetVelocity.x = (float)changes["_velocityX"];
                if (changes.ContainsKey("_velocityY")) _targetVelocity.y = (float)changes["_velocityY"];
                if (changes.ContainsKey("_velocityZ")) _targetVelocity.z = (float)changes["_velocityZ"];
            }
            if (_syncProperties)
            {
                if (changes.ContainsKey("_mass")) _mass = (float)changes["_mass"];
                if (changes.ContainsKey("_drag")) _drag = (float)changes["_drag"];
                if (changes.ContainsKey("_angularDrag")) _angularDrag = (float)changes["_angularDrag"];
                if (changes.ContainsKey("_useGravity")) _useGravity = (bool)changes["_useGravity"];
            }
            _pendingForces.Clear();
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            if (_syncPosition)
            {
                if (changes.ContainsKey("_positionX") && Mathf.Abs((float)changes["_positionX"] - _rigidbody.position.x) > _positionThreshold) return true;
                if (changes.ContainsKey("_positionY") && Mathf.Abs((float)changes["_positionY"] - _rigidbody.position.y) > _positionThreshold) return true;
                if (changes.ContainsKey("_positionZ") && Mathf.Abs((float)changes["_positionZ"] - _rigidbody.position.z) > _positionThreshold) return true;
            }
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX") && Mathf.Abs((float)changes["_rotationX"] - _rigidbody.rotation.x) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationY") && Mathf.Abs((float)changes["_rotationY"] - _rigidbody.rotation.y) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationZ") && Mathf.Abs((float)changes["_rotationZ"] - _rigidbody.rotation.z) > _rotationThreshold) return true;
                if (changes.ContainsKey("_rotationW") && Mathf.Abs((float)changes["_rotationW"] - _rigidbody.rotation.w) > _rotationThreshold) return true;
            }
            if (_syncVelocity)
            {
                if (changes.ContainsKey("_velocityX") && Mathf.Abs((float)changes["_velocityX"] - _rigidbody.velocity.x) > _velocityThreshold) return true;
                if (changes.ContainsKey("_velocityY") && Mathf.Abs((float)changes["_velocityY"] - _rigidbody.velocity.y) > _velocityThreshold) return true;
                if (changes.ContainsKey("_velocityZ") && Mathf.Abs((float)changes["_velocityZ"] - _rigidbody.velocity.z) > _velocityThreshold) return true;
            }
            return false;
        }

        protected override void Predict(float deltaTime, ObjectState lastState, List<InputCommand> lastInputs)
        {
            if (!isOwned || !_predictPhysics) return;

            float stepTime = _predictionTime / _predictionSteps;
            Vector3 currentPosition = _rigidbody.position;
            Vector3 currentVelocity = _rigidbody.velocity;
            Quaternion currentRotation = _rigidbody.rotation;
            Vector3 currentAngularVelocity = _rigidbody.angularVelocity;

            for (int i = 0; i < _predictionSteps; i++)
            {
                // Predict position
                currentPosition += currentVelocity * stepTime;
                if (_useGravity)
                {
                    currentVelocity += Physics.gravity * stepTime;
                }

                // Predict rotation
                currentRotation = Quaternion.Euler(currentAngularVelocity * (Mathf.Rad2Deg * stepTime)) * currentRotation;

                // Apply forces
                foreach (var force in _pendingForces)
                {
                    switch (force.Mode)
                    {
                        case ForceMode.Force:
                            currentVelocity += force.Force * stepTime / _mass;
                            break;
                        case ForceMode.Acceleration:
                            currentVelocity += force.Force * stepTime;
                            break;
                        case ForceMode.Impulse:
                            currentVelocity += force.Force / _mass;
                            break;
                        case ForceMode.VelocityChange:
                            currentVelocity += force.Force;
                            break;
                    }
                }
            }

            _targetPosition = currentPosition;
            _targetRotation = currentRotation;
            _targetVelocity = currentVelocity;
            _targetAngularVelocity = currentAngularVelocity;
        }

        private void FixedUpdate()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
            
            if (!isOwned)
            {
                if (_syncPosition)
                    _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
    
                if (_syncRotation)
                    _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);

                if (_syncVelocity)
                {
                    _targetVelocity = new Vector3(_velocityX, _velocityY, _velocityZ);
                    if (_smoothAngularVelocity)
                    {
                        Vector3 newAngularVelocity = new Vector3(_angularVelocityX, _angularVelocityY, _angularVelocityZ);
                        _targetAngularVelocity = Vector3.Lerp(_lastAngularVelocity, newAngularVelocity, _angularVelocitySmoothing);
                    }
                    else
                    {
                        _targetAngularVelocity = new Vector3(_angularVelocityX, _angularVelocityY, _angularVelocityZ);
                    }
                }
                
                float lerpSpeed = Time.deltaTime * _interpolationSpeed;
                float curveValue = _interpolationCurve.Evaluate(lerpSpeed);
                
                if (_syncPosition)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Vector3 velocity = (_targetPosition - _lastPosition) / Time.deltaTime;
                        _targetPosition += velocity * Time.deltaTime;
                    }

                    if (Vector3.Distance(_rigidbody.position, _targetPosition) > _maxTeleportDistance)
                    {
                        _rigidbody.MovePosition(_targetPosition);
                    }
                    else
                    {
                        _rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, _targetPosition, curveValue * _positionSmoothingFactor));
                    }
                }
                
                if (_syncRotation)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Quaternion deltaRotation = Quaternion.Inverse(_lastRotation) * _targetRotation;
                        _targetRotation = _lastRotation * Quaternion.Slerp(Quaternion.identity, deltaRotation, Time.deltaTime);
                    }

                    _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, _targetRotation, curveValue * _rotationSmoothingFactor));
                }

                if (_syncVelocity)
                {
                    if (_useVelocityBasedInterpolation)
                    {
                        Vector3 velocityDelta = (_targetVelocity - _lastVelocity) / Time.deltaTime;
                        _targetVelocity += velocityDelta * Time.deltaTime;
                    }

                    _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, _targetVelocity, curveValue * _velocitySmoothingFactor);
                    _rigidbody.angularVelocity = _targetAngularVelocity;
                }

                _lastPosition = _rigidbody.position;
                _lastRotation = _rigidbody.rotation;
                _lastVelocity = _rigidbody.velocity;
                _lastAngularVelocity = _rigidbody.angularVelocity;
            }
            else
            {
                SetState();
            }
        }

        private void SetState()
        {
            if (_syncPosition)
            {
                _positionX = Quantize(_rigidbody.position.x);
                _positionY = Quantize(_rigidbody.position.y);
                _positionZ = Quantize(_rigidbody.position.z);
            }
            if (_syncRotation)
            {
                _rotationX = Quantize(_rigidbody.rotation.x);
                _rotationY = Quantize(_rigidbody.rotation.y);
                _rotationZ = Quantize(_rigidbody.rotation.z);
                _rotationW = Quantize(_rigidbody.rotation.w);
            }
            if (_syncVelocity)
            {
                _velocityX = Quantize(_rigidbody.velocity.x);
                _velocityY = Quantize(_rigidbody.velocity.y);
                _velocityZ = Quantize(_rigidbody.velocity.z);
                _angularVelocityX = Quantize(_rigidbody.angularVelocity.x);
                _angularVelocityY = Quantize(_rigidbody.angularVelocity.y);
                _angularVelocityZ = Quantize(_rigidbody.angularVelocity.z);
            }
            if (_syncProperties)
            {
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _useGravity = _rigidbody.useGravity;
            }
        }
        private float Quantize(float value)
        {
            return Mathf.Round(value / syncPrecision) * syncPrecision;
        }
    }
} 