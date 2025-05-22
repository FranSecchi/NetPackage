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
        
        public static Action UpdatePrediction;
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
            UpdatePrediction.Invoke();
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
            var targetState = GetStateAtTime(targetTime);

            if (!targetState.HasValue)
            {
                _isRollingBack = false;
                return;
            }
            
            DebugQueue.AddMessage($"Rollback to {targetTime:F3}s", DebugQueue.MessageType.Rollback);
            // Restore the state
            foreach (var kvp in targetState.Value.Snapshot)
            {
                StateManager.RestoreState(kvp.Key, kvp.Value);
            }
            
            // Replay inputs that happened after the rollback point
            var inputsToReplay = GetInputsAtTime(targetTime);

            // Replay inputs
            foreach (var input in inputsToReplay)
            {
                RPCManager.CallRPC(input.NetId, input.MethodName, input.Parameters);
            }
            
            _isRollingBack = false;
        }



        public static void RollbackToTime(int netId, int id, float targetTime, Dictionary<string, object> changes)
        {
            if (_isRollingBack || _stateHistory.Count == 0)
            {
                DebugQueue.AddMessage($"Object {netId} failed to roll back to {targetTime:F3}s", DebugQueue.MessageType.Warning);
                return;
            }
            _isRollingBack = true;
            _lastRollbackTime = Time.time;
            
            var targetState = GetStateAtTime(targetTime);

            if (!targetState.HasValue)
            {
                _isRollingBack = false;
                return;
            }
            
            var state = targetState.Value.Snapshot[netId];
            StateManager.RestoreState(netId, state);
            
            var inputsToReplay = GetInputsAtTime(targetTime);
            
            foreach (var input in inputsToReplay)
            {
                if(input.NetId == netId)
                {
                    DebugQueue.AddMessage($"Re-applied input {input.MethodName}", DebugQueue.MessageType.Rollback);
                    RPCManager.CallRPC(input.NetId, input.MethodName, input.Parameters);
                }
            }
            state.SetChange(netId, id, changes);
        }
        
        public static void Clear()
        {
            _stateHistory.Clear();
            _inputBuffer.Clear();
            _isRollingBack = false;
        }
        private static GameState? GetStateAtTime(float targetTime)
        {
            GameState? targetState = null;
            foreach (var state in _stateHistory)
            {
                if (state.Timestamp <= targetTime)
                {
                    targetState = state;
                    break;
                }
            }
            if(targetState == null)
                DebugQueue.AddMessage($"Failed to find state to roll back to at time {targetTime:F3}s", DebugQueue.MessageType.Warning);
            return targetState;
        }
        private static List<InputCommand> GetInputsAtTime(float targetTime)
        {
            var inputsToReplay = new List<InputCommand>();
            foreach (var input in _inputBuffer)
            {
                if (input.Timestamp >= targetTime)
                {
                    inputsToReplay.Add(input);
                }
            }

            return inputsToReplay;
        }
    }
} 