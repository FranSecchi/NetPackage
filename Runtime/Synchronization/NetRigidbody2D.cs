using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetRigidBody2D : NetBehaviour
    {
        [Sync] private float _positionX;
        [Sync] private float _positionY;
        [Sync] private float _rotation;
        [Sync] private float _velocityX;
        [Sync] private float _velocityY;
        [Sync] private float _angularVelocity;
        [Sync] private float _mass;
        [Sync] private float _drag;
        [Sync] private float _angularDrag;
        [Sync] private float _gravityScale;
        [Sync] private bool _isKinematic;

        // Interpolation settings
        [SerializeField] private float _interpolationBackTime = 0.1f;
        [SerializeField] private float _interpolationSpeed = 10f;
        
        private bool _isSynchronized = true;
        private Rigidbody2D _rigidbody;
        private Vector2 _targetPosition;
        private float _targetRotation;
        private Vector2 _targetVelocity;
        private float _targetAngularVelocity;
        private bool _hasTargetState;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            base.Awake();
        }

        public void Reset()
        {
            _isSynchronized = false;
            _hasTargetState = false;
            
            // Update the sync variables
            _positionX = _rigidbody.position.x;
            _positionY = _rigidbody.position.y;
            _rotation = _rigidbody.rotation;
            _velocityX = _rigidbody.velocity.x;
            _velocityY = _rigidbody.velocity.y;
            _angularVelocity = _rigidbody.angularVelocity;
            _mass = _rigidbody.mass;
            _drag = _rigidbody.drag;
            _angularDrag = _rigidbody.angularDrag;
            _gravityScale = _rigidbody.gravityScale;
            _isKinematic = _rigidbody.isKinematic;
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
            _positionX = _rigidbody.position.x;
            _positionY = _rigidbody.position.y;
            _rotation = _rigidbody.rotation;
            _velocityX = _rigidbody.velocity.x;
            _velocityY = _rigidbody.velocity.y;
            _angularVelocity = _rigidbody.angularVelocity;
            _mass = _rigidbody.mass;
            _drag = _rigidbody.drag;
            _angularDrag = _rigidbody.angularDrag;
            _gravityScale = _rigidbody.gravityScale;
            _isKinematic = _rigidbody.isKinematic;

            // Initialize target state
            _targetPosition = _rigidbody.position;
            _targetRotation = _rigidbody.rotation;
            _targetVelocity = _rigidbody.velocity;
            _targetAngularVelocity = _rigidbody.angularVelocity;
            _hasTargetState = true;
        }

        protected override void OnStateReconcile(Dictionary<string, object> changes)
        {
            if (changes.ContainsKey("_positionX")) _positionX = (float)changes["_positionX"];
            if (changes.ContainsKey("_positionY")) _positionY = (float)changes["_positionY"];
            if (changes.ContainsKey("_rotation")) _rotation = (float)changes["_rotation"];
            if (changes.ContainsKey("_velocityX")) _velocityX = (float)changes["_velocityX"];
            if (changes.ContainsKey("_velocityY")) _velocityY = (float)changes["_velocityY"];
            if (changes.ContainsKey("_angularVelocity")) _angularVelocity = (float)changes["_angularVelocity"];
            if (changes.ContainsKey("_mass")) _mass = (float)changes["_mass"];
            if (changes.ContainsKey("_drag")) _drag = (float)changes["_drag"];
            if (changes.ContainsKey("_angularDrag")) _angularDrag = (float)changes["_angularDrag"];
            if (changes.ContainsKey("_gravityScale")) _gravityScale = (float)changes["_gravityScale"];
            if (changes.ContainsKey("_isKinematic")) _isKinematic = (bool)changes["_isKinematic"];

            // Update target state
            _targetPosition = new Vector2(_positionX, _positionY);
            _targetRotation = _rotation;
            _targetVelocity = new Vector2(_velocityX, _velocityY);
            _targetAngularVelocity = _angularVelocity;
            _hasTargetState = true;
        }

        protected override bool IsDesynchronized(Dictionary<string, object> changes)
        {
            if (!isOwned) return false;

            float positionThreshold = _desyncThreshold;
            float rotationThreshold = _desyncThreshold;
            float velocityThreshold = _desyncThreshold;
            float angularVelocityThreshold = _desyncThreshold;

            if (changes.ContainsKey("_positionX") && Mathf.Abs((float)changes["_positionX"] - _rigidbody.position.x) > positionThreshold) return true;
            if (changes.ContainsKey("_positionY") && Mathf.Abs((float)changes["_positionY"] - _rigidbody.position.y) > positionThreshold) return true;
            if (changes.ContainsKey("_rotation") && Mathf.Abs((float)changes["_rotation"] - _rigidbody.rotation) > rotationThreshold) return true;
            if (changes.ContainsKey("_velocityX") && Mathf.Abs((float)changes["_velocityX"] - _rigidbody.velocity.x) > velocityThreshold) return true;
            if (changes.ContainsKey("_velocityY") && Mathf.Abs((float)changes["_velocityY"] - _rigidbody.velocity.y) > velocityThreshold) return true;
            if (changes.ContainsKey("_angularVelocity") && Mathf.Abs((float)changes["_angularVelocity"] - _rigidbody.angularVelocity) > angularVelocityThreshold) return true;

            return false;
        }

        protected override void Predict(float deltaTime)
        {
            // For rigidbody, prediction is handled by the physics system
            // We just need to update our sync variables
            _positionX = _rigidbody.position.x;
            _positionY = _rigidbody.position.y;
            _rotation = _rigidbody.rotation;
            _velocityX = _rigidbody.velocity.x;
            _velocityY = _rigidbody.velocity.y;
            _angularVelocity = _rigidbody.angularVelocity;
            _mass = _rigidbody.mass;
            _drag = _rigidbody.drag;
            _angularDrag = _rigidbody.angularDrag;
            _gravityScale = _rigidbody.gravityScale;
            _isKinematic = _rigidbody.isKinematic;
        }

        private void FixedUpdate()
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
                    _rotation = GetFieldValue<float>("_rotation");
                    _velocityX = GetFieldValue<float>("_velocityX");
                    _velocityY = GetFieldValue<float>("_velocityY");
                    _mass = GetFieldValue<float>("_mass");
                    _angularVelocity = GetFieldValue<float>("_angularVelocity");
                    _drag = GetFieldValue<float>("_drag");
                    _angularDrag = GetFieldValue<float>("_angularDrag");
                    _gravityScale = GetFieldValue<float>("_gravityScale");
                    _isKinematic = GetFieldValue<bool>("_isKinematic");

                    // Update target state
                    _targetPosition = new Vector2(_positionX, _positionY);
                    _targetRotation = _rotation;
                    _targetVelocity = new Vector2(_velocityX, _velocityY);
                    _targetAngularVelocity = _angularVelocity;
                    _hasTargetState = true;
                }

                // Interpolate to target state
                if (_hasTargetState)
                {
                    _rigidbody.position = Vector2.Lerp(_rigidbody.position, _targetPosition, Time.fixedDeltaTime * _interpolationSpeed);
                    _rigidbody.rotation = Mathf.Lerp(_rigidbody.rotation, _targetRotation, Time.fixedDeltaTime * _interpolationSpeed);
                    _rigidbody.velocity = Vector2.Lerp(_rigidbody.velocity, _targetVelocity, Time.fixedDeltaTime * _interpolationSpeed);
                    _rigidbody.angularVelocity = Mathf.Lerp(_rigidbody.angularVelocity, _targetAngularVelocity, Time.fixedDeltaTime * _interpolationSpeed);
                }
            }
            else
            {
                // Update sync variables from current rigidbody state
                _positionX = _rigidbody.position.x;
                _positionY = _rigidbody.position.y;
                _rotation = _rigidbody.rotation;
                _velocityX = _rigidbody.velocity.x;
                _velocityY = _rigidbody.velocity.y;
                _angularVelocity = _rigidbody.angularVelocity;
                _mass = _rigidbody.mass;
                _drag = _rigidbody.drag;
                _angularDrag = _rigidbody.angularDrag;
                _gravityScale = _rigidbody.gravityScale;
                _isKinematic = _rigidbody.isKinematic;
            }
        }
    }
}