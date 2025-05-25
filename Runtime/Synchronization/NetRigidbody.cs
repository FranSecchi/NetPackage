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

        [Header("Desync Thresholds")]
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 0.01f;
        [SerializeField] private float _velocityThreshold = 0.1f;

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
        
        [Sync] private float _mass;
        [Sync] private float _drag;
        [Sync] private float _angularDrag;
        [Sync] private bool _useGravity;

        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private Rigidbody _rigidbody;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetVelocity;
        private List<ForceCommand> _pendingForces = new List<ForceCommand>();
        private bool _wasKinematic;
        private CollisionDetectionMode _wasCollisionDetection;
        private RigidbodyInterpolation _wasInterpolation;

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

            SendState();

            _targetPosition = _rigidbody.position;
            _targetRotation = _rigidbody.rotation;
            _targetVelocity = _rigidbody.velocity;
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
                if (changes.ContainsKey("_positionX")) _positionX = (float)changes["_positionX"];
                if (changes.ContainsKey("_positionY")) _positionY = (float)changes["_positionY"];
                if (changes.ContainsKey("_positionZ")) _positionZ = (float)changes["_positionZ"];            
                _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
            }
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX")) _rotationX = (float)changes["_rotationX"];
                if (changes.ContainsKey("_rotationY")) _rotationY = (float)changes["_rotationY"];
                if (changes.ContainsKey("_rotationZ")) _rotationZ = (float)changes["_rotationZ"];
                if (changes.ContainsKey("_rotationW")) _rotationW = (float)changes["_rotationW"];
                _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
            }
            if (_syncVelocity)
            {
                if (changes.ContainsKey("_velocityX")) _velocityX = (float)changes["_velocityX"];
                if (changes.ContainsKey("_velocityY")) _velocityY = (float)changes["_velocityY"];
                if (changes.ContainsKey("_velocityZ")) _velocityZ = (float)changes["_velocityZ"];
                _targetVelocity = new Vector3(_velocityX, _velocityY, _velocityZ);
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

        protected override void Predict(float deltaTime, ObjectState lastState, List<RollbackManager.InputCommand> lastInputs)
        {
            if (!isOwned) return;

            if (_syncPosition)
            {
                Vector3 p = _rigidbody.position + _rigidbody.velocity * Time.fixedDeltaTime +
                            (_useGravity ? Physics.gravity * (0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime) : Vector3.zero);
                _targetPosition = p;
            }
            if (_syncRotation)
            {
                Quaternion q = Quaternion.Euler(_rigidbody.angularVelocity * (Mathf.Rad2Deg * deltaTime)) * _rigidbody.rotation;
                _targetRotation = q;
            }
            if (_syncVelocity)
            {
                Vector3 v = _rigidbody.velocity + (_useGravity ? Physics.gravity * deltaTime : Vector3.zero);
                
                foreach (var force in _pendingForces)
                {
                    switch (force.Mode)
                    {
                        case ForceMode.Force:
                            v += force.Force * force.DeltaTime / _mass;
                            break;
                        case ForceMode.Acceleration:
                            v += force.Force * force.DeltaTime;
                            break;
                        case ForceMode.Impulse:
                            v += force.Force / _mass;
                            break;
                        case ForceMode.VelocityChange:
                            v += force.Force;
                            break;
                    }
                }
                
                _targetVelocity = v;
            }            
            SendState();
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
            }
            SetState();
        }

        private void SetState()
        {
            float lerpSpeed = Time.deltaTime * _interpolationSpeed;
            
            if (!isOwned)
            {
                if (_syncPosition)
                {
                    _rigidbody.MovePosition(Vector3.Lerp(_rigidbody.position, _targetPosition, lerpSpeed));
                }
                
                if (_syncRotation)
                {
                    _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, _targetRotation, lerpSpeed));
                }
            }
            else
            {
                if (_syncVelocity)
                    _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, _targetVelocity, lerpSpeed);
            }
        }

        private void SendState()
        {
            if (_syncPosition)
            {
                _positionX = _rigidbody.position.x;
                _positionY = _rigidbody.position.y;
                _positionZ = _rigidbody.position.z;
            }
            if (_syncRotation)
            {
                _rotationX = _rigidbody.rotation.x;
                _rotationY = _rigidbody.rotation.y;
                _rotationZ = _rigidbody.rotation.z;
                _rotationW = _rigidbody.rotation.w;
            }
            if (_syncVelocity)
            {
                _velocityX = _rigidbody.velocity.x;
                _velocityY = _rigidbody.velocity.y;
                _velocityZ = _rigidbody.velocity.z;
            }
            if (_syncProperties)
            {
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _useGravity = _rigidbody.useGravity;
            }
        }
    }
} 