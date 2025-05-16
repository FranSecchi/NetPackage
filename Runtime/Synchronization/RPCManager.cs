using System;
using System.Collections.Generic;
using System.Reflection;
using NetPackage.Runtime.NetworkManager;
using NetPackage.Runtime.Serializer;
using NetPackage.Runtime.Messages;
using UnityEngine;

namespace NetPackage.Runtime.Synchronization
{
    public class RPCManager
    {
        private static Dictionary<int, List<object>> _rpcTargets = new();
        private static Dictionary<int, Dictionary<string, List<MethodInfo>>> _rpcMethods = new();
        
        public static void Register(int netId, object target)
        {
            if (!_rpcTargets.ContainsKey(netId))
            {
                _rpcTargets[netId] = new List<object>();
                _rpcMethods[netId] = new Dictionary<string, List<MethodInfo>>();
            }

            if (_rpcTargets[netId].Contains(target))
                return;

            _rpcTargets[netId].Add(target);

            var methods = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var rpcAttr = method.GetCustomAttribute<NetRPC>();
                if (rpcAttr != null)
                {
                    if (!_rpcMethods[netId].ContainsKey(method.Name))
                    {
                        _rpcMethods[netId][method.Name] = new List<MethodInfo>();
                    }
                    _rpcMethods[netId][method.Name].Add(method);
                }
            }
        }

        public static void Unregister(int netId, object target)
        {
            if (!_rpcTargets.ContainsKey(netId))
                return;

            _rpcTargets[netId].Remove(target);

            foreach (var methodList in _rpcMethods[netId].Values)
            {
                methodList.RemoveAll(m => m.DeclaringType == target.GetType());
            }

            var emptyMethods = new List<string>();
            foreach (var kvp in _rpcMethods[netId])
            {
                if (kvp.Value.Count == 0)
                {
                    emptyMethods.Add(kvp.Key);
                }
            }
            foreach (var methodName in emptyMethods)
            {
                _rpcMethods[netId].Remove(methodName);
            }
            
            if (_rpcTargets[netId].Count == 0)
            {
                _rpcTargets.Remove(netId);
                _rpcMethods.Remove(netId);
            }
        }

        public static void CallRPC(RPCMessage message)
        {
            CallRPC(message.ObjectId, message.MethodName, message.Parameters);
        }

        public static void CallRPC(int netId, string methodName, object[] parameters)
        {
            if (!_rpcTargets.ContainsKey(netId))
            {
                Debug.LogWarning($"No RPC targets found for netId {netId}");
                return;
            }

            if (!_rpcMethods[netId].ContainsKey(methodName))
            {
                Debug.LogWarning($"No RPC method {methodName} found for netId {netId}");
                return;
            }
            
            foreach (var method in _rpcMethods[netId][methodName])
            {
                try
                {
                    var target = _rpcTargets[netId].Find(t => t.GetType() == method.DeclaringType);
                    if (target != null)
                    {
                        var paramTypes = method.GetParameters();
                        var convertedParams = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            var p = parameters[i];
                            if (p == null)
                            {
                                convertedParams[i] = null;
                            }
                            else if (paramTypes[i].ParameterType.IsInstanceOfType(p))
                            {
                                convertedParams[i] = p;
                            }
                            else
                            {
                                var bytes = NetSerializer._Serializer.Serialize(p);
                                convertedParams[i] = NetSerializer._Serializer.Deserialize(bytes, paramTypes[i].ParameterType);
                            }
                        }
                        method.Invoke(target, convertedParams);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error invoking RPC {methodName}: {e}");
                }
            }
        }

        public static void SendRPC(int netId, string methodName, params object[] parameters)
        {
            if (!_rpcTargets.ContainsKey(netId))
            {
                Debug.LogWarning($"No RPC targets found for netId {netId}");
                return;
            }

            if (!_rpcMethods[netId].ContainsKey(methodName))
            {
                Debug.LogWarning($"No RPC method {methodName} found for netId {netId}");
                return;
            }
            List<int> targetIds = null;
            
            foreach (var method in _rpcMethods[netId][methodName])
            {
                var rpcAttr = method.GetCustomAttribute<NetRPC>();
                if (rpcAttr != null)
                {
                    if (rpcAttr.Direction == Direction.ServerToClient && !NetManager.IsHost)
                    {
                        Debug.LogWarning($"Cannot send RPC {methodName} - it is server-to-client only");
                        return;
                    }
                    if (rpcAttr.Direction == Direction.ClientToServer && NetManager.IsHost)
                    {
                        Debug.LogWarning($"Cannot send RPC {methodName} - it is client-to-server only");
                        return;
                    }
                    
                    switch(rpcAttr.TargetMode)
                    {
                        case Send.Specific:
                            if (parameters[^1].GetType() != typeof(List<int>))
                            {
                                Debug.LogWarning(
                                    $"Cannot send RPC {methodName} - the last parameter should be the target clients as a List<int> and is {parameters[^1].GetType()}");
                                return;
                            }
                            targetIds = (List<int>)parameters[^1];
                            break;
                        case Send.Others:
                            targetIds = new List<int>(NetManager.allPlayers);
                            targetIds.Remove(NetManager.ConnectionId());
                            break;
                        case Send.All:
                            CallRPC(netId, methodName, parameters);
                            break;
                    }
                }
            }
            var message = new RPCMessage(NetManager.ConnectionId(), netId, methodName, targetIds, parameters);
            NetManager.Send(message);
        }
    }
} 