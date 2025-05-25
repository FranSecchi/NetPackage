using System;
using System.Collections.Generic;
using NetPackage.Messages;
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
        
        internal static Action<GameState> UpdatePrediction;

        internal struct GameState
        {
            public DateTime Timestamp;
            public Dictionary<int, ObjectState> Snapshot;
            public List<InputCommand> Inputs;
        }
        
        public struct InputCommand
        {
            public int NetId;
            public string MethodName;
            public object[] Parameters;
            public DateTime Timestamp;
        }
        
        public static void Initialize(float rollbackWindow = 0.1f, int maxStates = 20)
        {
            _rollbackWindow = rollbackWindow;
            _maxStates = maxStates;
            _stateHistory.Clear();
            _inputBuffer.Clear();
            Messager.RegisterHandler<ReconcileMessage>(OnReconcileMessage);
        }

        private static void OnReconcileMessage(ReconcileMessage obj)
        {
            if (_stateHistory.Count == 0)
                return;

            var targetState = GetStateAtTime(obj.Timestamp);
            if (!targetState.HasValue)
            {
                DebugQueue.AddMessage($"Failed to find state for reconciliation of object {obj.ObjectId} at time {obj.Timestamp:HH:mm:ss.fff}", DebugQueue.MessageType.Rollback);
                return;
            }

            if (!targetState.Value.Snapshot.TryGetValue(obj.ObjectId, out var state))
            {
                DebugQueue.AddMessage($"Object {obj.ObjectId} not found in state snapshot at time {obj.Timestamp:HH:mm:ss.fff}", DebugQueue.MessageType.Rollback);
                return;
            }

            if (!state.HasComponent(obj.ComponentId))
            {
                DebugQueue.AddMessage($"Component {obj.ComponentId} not found in object {obj.ObjectId} at time {obj.Timestamp:HH:mm:ss.fff}", DebugQueue.MessageType.Rollback);
                return;
            }

            var timeDiff = (DateTime.UtcNow - obj.Timestamp).TotalSeconds;
            
            // Perform the reconciliation
            state.Reconcile(obj.ObjectId, obj.ComponentId, obj.Values, obj.Timestamp);
            
            DebugQueue.AddMessage($"Reconciled object {obj.ObjectId} component {obj.ComponentId} at time {obj.Timestamp:HH:mm:ss.fff} (time diff: {timeDiff:F3}s)", DebugQueue.MessageType.Rollback);
        }

        public static void Update()
        {
            if(_stateHistory.Count > 0) UpdatePrediction?.Invoke(_stateHistory.Peek());
            StoreCurrentState();
            CleanupOldStates();
        }
        
        private static void StoreCurrentState()
        {
            var currentTime = DateTime.UtcNow;
            
            // Remove states older than rollback window
            while (_stateHistory.Count > 0)
            {
                var oldestState = _stateHistory.Peek();
                if ((currentTime - oldestState.Timestamp).TotalSeconds > _rollbackWindow)
                {
                    var oldState = _stateHistory.Dequeue();
                    oldState.Snapshot.Clear();
                    oldState.Inputs.Clear();
                }
                else
                {
                    break;
                }
            }
            
            // Add new state if we haven't reached max states
            if (_stateHistory.Count < _maxStates)
            {
                var currentState = new GameState
                {
                    Timestamp = currentTime,
                    Snapshot = new Dictionary<int, ObjectState>(),
                    Inputs = new List<InputCommand>()
                };
                
                foreach (var kvp in StateManager.GetAllStates())
                {
                    currentState.Snapshot[kvp.Key] = kvp.Value.Clone();
                }
                
                _stateHistory.Enqueue(currentState);
            }
        }

        private static void CleanupOldStates()
        {
            TimeSpan spanned = _stateHistory.Peek().Timestamp - DateTime.UtcNow;
            
            while (_stateHistory.Count > 0 && spanned.TotalMilliseconds > _rollbackWindow)
            {
                var oldState = _stateHistory.Dequeue();
                oldState.Snapshot.Clear();
                oldState.Inputs.Clear();
            }
            
            while (_inputBuffer.Count > 0 && spanned.TotalMilliseconds > _rollbackWindow)
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
                Timestamp = DateTime.UtcNow
            };
            
            _inputBuffer.Enqueue(input);
            
            if (_stateHistory.Count > 0)
            {
                var currentState = _stateHistory.Peek();
                currentState.Inputs.Add(input);
            }
        }
        
        public static void RollbackToTime(DateTime targetTime)
        {
            var targetState = GetStateAtTime(targetTime);
            if (!targetState.HasValue)
            {
                DebugQueue.AddMessage($"Failed to find state for rollback at time {targetTime:F3}s", DebugQueue.MessageType.Rollback);
                return;
            }
            
            DebugQueue.AddMessage($"Starting rollback to {targetTime:F3}s", DebugQueue.MessageType.Rollback);
            
            foreach (var kvp in targetState.Value.Snapshot)
            {
                StateManager.RestoreState(kvp.Key, kvp.Value);
            }
            
            var inputsToReplay = GetInputsAtTime(targetTime);
            foreach (var input in inputsToReplay)
            {
                RPCManager.CallRPC(input.NetId, input.MethodName, input.Parameters);
            }
            DebugQueue.AddMessage($"Rollback completed, replaying {inputsToReplay.Count} inputs", DebugQueue.MessageType.Rollback);
        }

        public static void RollbackToTime(int netId, int id, DateTime targetTime, Dictionary<string, object> changes)
        {
            if (_stateHistory.Count == 0)
            {
                DebugQueue.AddMessage($"Object {netId} failed to roll back to {targetTime:F3}s - Invalid state", DebugQueue.MessageType.Rollback);
                return;
            }
            
            var targetState = GetStateAtTime(targetTime);
            if (!targetState.HasValue)
            {
                DebugQueue.AddMessage($"Object {netId} failed to roll back to {targetTime:F3}s - No state found", DebugQueue.MessageType.Rollback);
                return;
            }
            
            DebugQueue.AddMessage($"Starting object-specific rollback for {netId} to {targetTime:F3}s", DebugQueue.MessageType.Rollback);
            
            if (targetState.Value.Snapshot.TryGetValue(netId, out var state))
            {
                StateManager.RestoreState(netId, state);
                
                var inputsToReplay = GetInputsAtTime(targetTime);
                int replayedInputs = 0;
                foreach (var input in inputsToReplay)
                {
                    if (input.NetId == netId)
                    {
                        RPCManager.CallRPC(input.NetId, input.MethodName, input.Parameters);
                        replayedInputs++;
                    }
                }
                state.SetChange(id, changes);
                DebugQueue.AddMessage($"Object {netId} rollback completed, replayed {replayedInputs} inputs", DebugQueue.MessageType.Rollback);
            }
        }
        
        public static void Clear()
        {
            int stateCount = _stateHistory.Count;
            int inputCount = _inputBuffer.Count;
            
            foreach (var state in _stateHistory)
            {
                state.Snapshot.Clear();
                state.Inputs.Clear();
            }
            _stateHistory.Clear();
            _inputBuffer.Clear();
            DebugQueue.AddMessage($"RollbackManager cleared: {stateCount} states and {inputCount} inputs removed", DebugQueue.MessageType.Rollback);
        }

        private static GameState? GetStateAtTime(DateTime targetTime)
        {
            if (_stateHistory.Count == 0) return null;
            
            GameState? closestState = null;
            TimeSpan closestDiff = TimeSpan.MaxValue;
            
            foreach (var state in _stateHistory)
            {
                var diff = (state.Timestamp - targetTime).Duration();
                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestState = state;
                }
            }
            
            if (closestState == null || closestDiff.TotalSeconds > _rollbackWindow)
            {
                DebugQueue.AddMessage($"Failed to find state to roll back to at time {targetTime:HH:mm:ss.fff} (closest diff: {closestDiff.TotalMilliseconds}ms)", DebugQueue.MessageType.Rollback);
                return null;
            }
            
            return closestState;
        }

        private static List<InputCommand> GetInputsAtTime(DateTime targetTime)
        {
            if (_inputBuffer.Count == 0) return null;
            
            var inputsToReplay = new List<InputCommand>();
            TimeSpan closestDiff = TimeSpan.MaxValue;
            foreach (var input in _inputBuffer)
            {
                var diff = (input.Timestamp - targetTime).Duration();
                if (diff < closestDiff)
                {
                    inputsToReplay.Add(input);
                }
            }
            return inputsToReplay;
        }
    }
} 