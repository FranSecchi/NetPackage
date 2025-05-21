using System;
using System.Collections.Generic;
using NetPackage.Network;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public static class RollbackManager
    {
        private static float _rollbackWindow = 0.1f; // How far back to store states
        private static int _maxStates = 20; // Maximum number of states to store
        
        public static bool IsRollingBack => _isRollingBack;
        public static float LastRollbackTime => _lastRollbackTime;
        public static int StateCount => _stateHistory.Count;
        
        private struct GameState
        {
            public float Timestamp;
            public Dictionary<int, ObjectState> Snapshot;
            public List<InputCommand> Inputs;
        }
        
        private struct InputCommand
        {
            public int NetId;
            public string MethodName;
            public object[] Parameters;
            public float Timestamp;
        }
        
        private static Queue<GameState> _stateHistory = new Queue<GameState>();
        private static Queue<InputCommand> _inputBuffer = new Queue<InputCommand>();
        private static float _lastRollbackTime;
        private static bool _isRollingBack;
        
        public static void Initialize(float rollbackWindow = 0.1f, int maxStates = 20)
        {
            _rollbackWindow = rollbackWindow;
            _maxStates = maxStates;
            _stateHistory.Clear();
            _inputBuffer.Clear();
            _isRollingBack = false;
        }
        
        public static void Update()
        {
            if (!_isRollingBack)
            {
                // Store current state
                StoreCurrentState();
                
                // Clean up old states
                while (_stateHistory.Count > 0 && 
                       _stateHistory.Peek().Timestamp < Time.time - _rollbackWindow)
                {
                    _stateHistory.Dequeue();
                }
                
                // Clean up old inputs
                while (_inputBuffer.Count > 0 && 
                       _inputBuffer.Peek().Timestamp < Time.time - _rollbackWindow)
                {
                    _inputBuffer.Dequeue();
                }
            }
        }
        
        private static void StoreCurrentState()
        {
            if (_stateHistory.Count >= _maxStates)
            {
                _stateHistory.Dequeue();
            }
            
            var currentState = new GameState
            {
                Timestamp = Time.time,
                Snapshot = new Dictionary<int, ObjectState>(),
                Inputs = new List<InputCommand>()
            };
            
            // Clone all current states
            foreach (var kvp in StateManager.GetAllStates())
            {
                currentState.Snapshot[kvp.Key] = kvp.Value.Clone();
            }
            
            _stateHistory.Enqueue(currentState);
        }
        
        public static void RecordInput(int netId, string methodName, params object[] parameters)
        {
            var input = new InputCommand
            {
                NetId = netId,
                MethodName = methodName,
                Parameters = parameters,
                Timestamp = Time.time
            };
            
            _inputBuffer.Enqueue(input);
            
            // Also store in current state
            if (_stateHistory.Count > 0)
            {
                var currentState = _stateHistory.Peek();
                currentState.Inputs.Add(input);
            }
        }
        
        public static void RollbackToTime(float targetTime)
        {
            if (_isRollingBack) return;
            _isRollingBack = true;
            _lastRollbackTime = Time.time;
            
            // Find the closest state to roll back to
            GameState? targetState = null;
            foreach (var state in _stateHistory)
            {
                if (state.Timestamp <= targetTime)
                {
                    targetState = state;
                    break;
                }
            }
            
            if (!targetState.HasValue)
            {
                _isRollingBack = false;
                return;
            }
            
            // Restore the state
            foreach (var kvp in targetState.Value.Snapshot)
            {
                StateManager.RestoreState(kvp.Key, kvp.Value);
            }
            
            // Replay inputs that happened after the rollback point
            var inputsToReplay = new List<InputCommand>();
            foreach (var input in _inputBuffer)
            {
                if (input.Timestamp >= targetTime)
                {
                    inputsToReplay.Add(input);
                }
            }
            
            // Replay inputs
            foreach (var input in inputsToReplay)
            {
                RPCManager.SendRPC(input.NetId, input.MethodName, input.Parameters);
            }
            
            _isRollingBack = false;
        }
        
        public static void RollbackToState(int stateIndex)
        {
            if (_isRollingBack || stateIndex >= _stateHistory.Count) return;
            
            var states = _stateHistory.ToArray();
            var targetState = states[stateIndex];
            RollbackToTime(targetState.Timestamp);
        }
        
        public static void Clear()
        {
            _stateHistory.Clear();
            _inputBuffer.Clear();
            _isRollingBack = false;
        }
    }
} 