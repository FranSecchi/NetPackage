using System;
using System.Collections.Generic;
using NetPackage.Network;
using NetPackage.Utilities;
using UnityEngine;

namespace NetPackage.Synchronization
{
    public static class RollbackManager
    {
        private static float _rollbackWindow = 0.1f;
        private static int _maxStates = 20;
        private static readonly Queue<GameState> _stateHistory = new Queue<GameState>();
        private static readonly Queue<InputCommand> _inputBuffer = new Queue<InputCommand>();
        private static float _lastRollbackTime;
        private static bool _isRollingBack;
        private static readonly Dictionary<int, ObjectState> _tempSnapshot = new Dictionary<int, ObjectState>();
        
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
                StoreCurrentState();
                CleanupOldStates();
            }
            UpdatePrediction?.Invoke();
        }
        
        private static void StoreCurrentState()
        {
            if (_stateHistory.Count >= _maxStates)
            {
                var oldState = _stateHistory.Dequeue();
                oldState.Snapshot.Clear();
                oldState.Inputs.Clear();
            }
            
            var currentState = new GameState
            {
                Timestamp = Time.time,
                Snapshot = new Dictionary<int, ObjectState>(),
                Inputs = new List<InputCommand>()
            };
            
            foreach (var kvp in StateManager.GetAllStates())
            {
                currentState.Snapshot[kvp.Key] = kvp.Value.Clone();
            }
            
            _stateHistory.Enqueue(currentState);
        }

        private static void CleanupOldStates()
        {
            float cutoffTime = Time.time - _rollbackWindow;
            
            while (_stateHistory.Count > 0 && _stateHistory.Peek().Timestamp < cutoffTime)
            {
                var oldState = _stateHistory.Dequeue();
                oldState.Snapshot.Clear();
                oldState.Inputs.Clear();
            }
            
            while (_inputBuffer.Count > 0 && _inputBuffer.Peek().Timestamp < cutoffTime)
            {
                _inputBuffer.Dequeue();
            }
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
            
            if (_stateHistory.Count > 0)
            {
                var currentState = _stateHistory.Peek();
                currentState.Inputs.Add(input);
            }
        }
        
        public static void RollbackToTime(float targetTime)
        {
            if (_isRollingBack) return;
            
            var targetState = GetStateAtTime(targetTime);
            if (!targetState.HasValue)
            {
                return;
            }
            
            _isRollingBack = true;
            _lastRollbackTime = Time.time;
            
            DebugQueue.AddMessage($"Rollback to {targetTime:F3}s", DebugQueue.MessageType.Rollback);
            
            foreach (var kvp in targetState.Value.Snapshot)
            {
                StateManager.RestoreState(kvp.Key, kvp.Value);
            }
            
            var inputsToReplay = GetInputsAtTime(targetTime);
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
            
            var targetState = GetStateAtTime(targetTime);
            if (!targetState.HasValue)
            {
                return;
            }
            
            _isRollingBack = true;
            _lastRollbackTime = Time.time;
            
            if (targetState.Value.Snapshot.TryGetValue(netId, out var state))
            {
                StateManager.RestoreState(netId, state);
                
                var inputsToReplay = GetInputsAtTime(targetTime);
                foreach (var input in inputsToReplay)
                {
                    if (input.NetId == netId)
                    {
                        DebugQueue.AddMessage($"Re-applied input {input.MethodName}", DebugQueue.MessageType.Rollback);
                        RPCManager.CallRPC(input.NetId, input.MethodName, input.Parameters);
                    }
                }
                state.SetChange(id, changes);
            }
            
            _isRollingBack = false;
        }
        
        public static void Clear()
        {
            foreach (var state in _stateHistory)
            {
                state.Snapshot.Clear();
                state.Inputs.Clear();
            }
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
            
            if (targetState == null)
            {
                DebugQueue.AddMessage($"Failed to find state to roll back to at time {targetTime:F3}s", DebugQueue.MessageType.Warning);
            }
            
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