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
        [Sync] private bool _isKinematic;

        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private bool _isSynchronized = true;
        private Rigidbody _rigidbody;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetVelocity;

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

        public override void PausePrediction()
        {
            _isSynchronized = false;
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

            if(!isOwned)
            {
                _rigidbody.isKinematic = true;
                _syncProperties = false;
            }

            SendState();

            _targetPosition = _rigidbody.position;
            _targetRotation = _rigidbody.rotation;
            _targetVelocity = _rigidbody.velocity;
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

        protected override void Predict(float deltaTime)
        {
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
                Vector3 v =  _rigidbody.velocity + (_useGravity ? Physics.gravity * deltaTime : Vector3.zero);
                _targetVelocity = v;
            }
        }

        private void FixedUpdate()
        {
            if (!NetManager.Active || !NetManager.Running || !_isSynchronized)
                return;

            
            SetState();
            if (isOwned)
            {
                SendState();
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
                _isKinematic = _rigidbody.isKinematic;
            }
        }

        private void SetState()
        {
            if (_syncPosition)
            {
                _targetPosition = new Vector3(_positionX, _positionY, _positionZ);
                _rigidbody.position = Vector3.Lerp(_rigidbody.position, _targetPosition, Time.fixedDeltaTime * _interpolationSpeed);
            }
            if (_syncRotation)
            {                    
                _targetRotation = new Quaternion(_rotationX, _rotationY, _rotationZ, _rotationW);
                _rigidbody.rotation = Quaternion.Slerp(_rigidbody.rotation, _targetRotation, Time.fixedDeltaTime * _interpolationSpeed);
            }
            if (_syncVelocity)
            {
                _targetVelocity = new Vector3(_velocityX, _velocityY, _velocityZ);
                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, _targetVelocity, Time.fixedDeltaTime * _interpolationSpeed);
            }
            _rigidbody.isKinematic = _isKinematic;
            _rigidbody.drag = _drag;
            _rigidbody.angularDrag = _angularDrag;
            _rigidbody.useGravity = _useGravity;
            _rigidbody.mass = _mass;
        }
    }
} 