using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [RequireComponent(typeof(Rigidbody))]
    public class NetRigidBody : NetBehaviour
    {
        [Header("Synchronization Settings")]
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncVelocity = true;
        [SerializeField] private bool _syncAngularVelocity = true;
        [SerializeField] private bool _syncProperties = true;

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
        [Sync] private bool _isKinematic;

        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private bool _isSynchronized = true;
        private Rigidbody _rigidbody;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetVelocity;
        private Vector3 _targetAngularVelocity;
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

        public bool SyncVelocity
        {
            get => _syncVelocity;
            set => _syncVelocity = value;
        }

        public bool SyncAngularVelocity
        {
            get => _syncAngularVelocity;
            set => _syncAngularVelocity = value;
        }

        public bool SyncProperties
        {
            get => _syncProperties;
            set => _syncProperties = value;
        }

        public override void PausePrediction()
        {
            _isSynchronized = false;
            _hasTargetState = false;
            
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
            if (_syncAngularVelocity)
            {
                _angularVelocityX = _rigidbody.angularVelocity.x;
                _angularVelocityY = _rigidbody.angularVelocity.y;
                _angularVelocityZ = _rigidbody.angularVelocity.z;
            }
            if (_syncProperties)
            {
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _useGravity = _rigidbody.useGravity;
                _isKinematic = _rigidbody.isKinematic;
            }
        }

        public override void ResumePrediction()
        {
            _isSynchronized = true;
        }

        protected override void OnNetSpawn()
        {
            if (!NetManager.Active || !NetManager.Running)
                return;
            _rigidbody = GetComponent<Rigidbody>();

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
            if (_syncAngularVelocity)
            {
                _angularVelocityX = _rigidbody.angularVelocity.x;
                _angularVelocityY = _rigidbody.angularVelocity.y;
                _angularVelocityZ = _rigidbody.angularVelocity.z;
            }
            if (_syncProperties)
            {
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _useGravity = _rigidbody.useGravity;
                _isKinematic = _rigidbody.isKinematic;
            }

            _targetPosition = _rigidbody.position;
            _targetRotation = _rigidbody.rotation;
            _targetVelocity = _rigidbody.velocity;
            _targetAngularVelocity = _rigidbody.angularVelocity;
            _hasTargetState = true;
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            if (_syncPosition)
            {
                if (changes.ContainsKey("_positionX")) _positionX = (float)changes["_positionX"];
                if (changes.ContainsKey("_positionY")) _positionY = (float)changes["_positionY"];
                if (changes.ContainsKey("_positionZ")) _positionZ = (float)changes["_positionZ"];
            }
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX")) _rotationX = (float)changes["_rotationX"];
                if (changes.ContainsKey("_rotationY")) _rotationY = (float)changes["_rotationY"];
                if (changes.ContainsKey("_rotationZ")) _rotationZ = (float)changes["_rotationZ"];
                if (changes.ContainsKey("_rotationW")) _rotationW = (float)changes["_rotationW"];
            }
            if (_syncVelocity)
            {
                if (changes.ContainsKey("_velocityX")) _velocityX = (float)changes["_velocityX"];
                if (changes.ContainsKey("_velocityY")) _velocityY = (float)changes["_velocityY"];
                if (changes.ContainsKey("_velocityZ")) _velocityZ = (float)changes["_velocityZ"];
            }
            if (_syncAngularVelocity)
            {
                if (changes.ContainsKey("_angularVelocityX")) _angularVelocityX = (float)changes["_angularVelocityX"];
                if (changes.ContainsKey("_angularVelocityY")) _angularVelocityY = (float)changes["_angularVelocityY"];
                if (changes.ContainsKey("_angularVelocityZ")) _angularVelocityZ = (float)changes["_angularVelocityZ"];
            }
            if (_syncProperties)
            {
                if (changes.ContainsKey("_mass")) _mass = (float)changes["_mass"];
                if (changes.ContainsKey("_drag")) _drag = (float)changes["_drag"];
                if (changes.ContainsKey("_angularDrag")) _angularDrag = (float)changes["_angularDrag"];
                if (changes.ContainsKey("_useGravity")) _useGravity = (bool)changes["_useGravity"];
                if (changes.ContainsKey("_isKinematic")) _isKinematic = (bool)changes["_isKinematic"];
            }

            _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
            _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
            _targetVelocity = new Vector3(_velocityX, _velocityY, _velocityZ);
            _targetAngularVelocity = new Vector3(_angularVelocityX, _angularVelocityY, _angularVelocityZ);
            _hasTargetState = true;
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            float positionThreshold = _desyncThreshold;
            float rotationThreshold = _desyncThreshold;
            float velocityThreshold = _desyncThreshold;
            float angularVelocityThreshold = _desyncThreshold;

            if (_syncPosition)
            {
                if (changes.ContainsKey("_positionX") && Mathf.Abs((float)changes["_positionX"] - _rigidbody.position.x) > positionThreshold) return true;
                if (changes.ContainsKey("_positionY") && Mathf.Abs((float)changes["_positionY"] - _rigidbody.position.y) > positionThreshold) return true;
                if (changes.ContainsKey("_positionZ") && Mathf.Abs((float)changes["_positionZ"] - _rigidbody.position.z) > positionThreshold) return true;
            }
            if (_syncRotation)
            {
                if (changes.ContainsKey("_rotationX") && Mathf.Abs((float)changes["_rotationX"] - _rigidbody.rotation.x) > rotationThreshold) return true;
                if (changes.ContainsKey("_rotationY") && Mathf.Abs((float)changes["_rotationY"] - _rigidbody.rotation.y) > rotationThreshold) return true;
                if (changes.ContainsKey("_rotationZ") && Mathf.Abs((float)changes["_rotationZ"] - _rigidbody.rotation.z) > rotationThreshold) return true;
                if (changes.ContainsKey("_rotationW") && Mathf.Abs((float)changes["_rotationW"] - _rigidbody.rotation.w) > rotationThreshold) return true;
            }
            if (_syncVelocity)
            {
                if (changes.ContainsKey("_velocityX") && Mathf.Abs((float)changes["_velocityX"] - _rigidbody.velocity.x) > velocityThreshold) return true;
                if (changes.ContainsKey("_velocityY") && Mathf.Abs((float)changes["_velocityY"] - _rigidbody.velocity.y) > velocityThreshold) return true;
                if (changes.ContainsKey("_velocityZ") && Mathf.Abs((float)changes["_velocityZ"] - _rigidbody.velocity.z) > velocityThreshold) return true;
            }
            if (_syncAngularVelocity)
            {
                if (changes.ContainsKey("_angularVelocityX") && Mathf.Abs((float)changes["_angularVelocityX"] - _rigidbody.angularVelocity.x) > angularVelocityThreshold) return true;
                if (changes.ContainsKey("_angularVelocityY") && Mathf.Abs((float)changes["_angularVelocityY"] - _rigidbody.angularVelocity.y) > angularVelocityThreshold) return true;
                if (changes.ContainsKey("_angularVelocityZ") && Mathf.Abs((float)changes["_angularVelocityZ"] - _rigidbody.angularVelocity.z) > angularVelocityThreshold) return true;
            }

            return false;
        }

        protected override void Predict(float deltaTime)
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
            if (_syncAngularVelocity)
            {
                _angularVelocityX = _rigidbody.angularVelocity.x;
                _angularVelocityY = _rigidbody.angularVelocity.y;
                _angularVelocityZ = _rigidbody.angularVelocity.z;
            }
            if (_syncProperties)
            {
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _useGravity = _rigidbody.useGravity;
                _isKinematic = _rigidbody.isKinematic;
            }
        }

        private void FixedUpdate()
        {
            if (!NetManager.Active || !NetManager.Running || !_isSynchronized)
                return;

            if (!isOwned)
            {
                if (NetObject?.State != null)
                {
                    if (_syncPosition)
                    {
                        _positionX = GetFieldValue<float>("_positionX");
                        _positionY = GetFieldValue<float>("_positionY");
                        _positionZ = GetFieldValue<float>("_positionZ");
                    }
                    if (_syncRotation)
                    {
                        _rotationX = GetFieldValue<float>("_rotationX");
                        _rotationY = GetFieldValue<float>("_rotationY");
                        _rotationZ = GetFieldValue<float>("_rotationZ");
                        _rotationW = GetFieldValue<float>("_rotationW");
                    }
                    if (_syncVelocity)
                    {
                        _velocityX = GetFieldValue<float>("_velocityX");
                        _velocityY = GetFieldValue<float>("_velocityY");
                        _velocityZ = GetFieldValue<float>("_velocityZ");
                    }
                    if (_syncAngularVelocity)
                    {
                        _angularVelocityX = GetFieldValue<float>("_angularVelocityX");
                        _angularVelocityY = GetFieldValue<float>("_angularVelocityY");
                        _angularVelocityZ = GetFieldValue<float>("_angularVelocityZ");
                    }
                    if (_syncProperties)
                    {
                        _mass = GetFieldValue<float>("_mass");
                        _drag = GetFieldValue<float>("_drag");
                        _angularDrag = GetFieldValue<float>("_angularDrag");
                        _useGravity = GetFieldValue<bool>("_useGravity");
                        _isKinematic = GetFieldValue<bool>("_isKinematic");
                    }

                    _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
                    _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
                    _targetVelocity = new Vector3(_velocityX, _velocityY, _velocityZ);
                    _targetAngularVelocity = new Vector3(_angularVelocityX, _angularVelocityY, _angularVelocityZ);
                    _hasTargetState = true;
                }

                if (_hasTargetState)
                {
                    if (_syncPosition)
                        _rigidbody.position = Vector3.Lerp(_rigidbody.position, _targetPosition, Time.fixedDeltaTime * _interpolationSpeed);
                    if (_syncRotation)
                        _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, _targetRotation, Time.fixedDeltaTime * _interpolationSpeed);
                    if (_syncVelocity)
                        _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, _targetVelocity, Time.fixedDeltaTime * _interpolationSpeed);
                    if (_syncAngularVelocity)
                        _rigidbody.angularVelocity = Vector3.Lerp(_rigidbody.angularVelocity, _targetAngularVelocity, Time.fixedDeltaTime * _interpolationSpeed);
                }
            }
            else
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
                if (_syncAngularVelocity)
                {
                    _angularVelocityX = _rigidbody.angularVelocity.x;
                    _angularVelocityY = _rigidbody.angularVelocity.y;
                    _angularVelocityZ = _rigidbody.angularVelocity.z;
                }
                if (_syncProperties)
                {
                    _mass = _rigidbody.mass;
                    _drag = _rigidbody.drag;
                    _angularDrag = _rigidbody.angularDrag;
                    _useGravity = _rigidbody.useGravity;
                    _isKinematic = _rigidbody.isKinematic;
                }
            }
        }
    }
} 