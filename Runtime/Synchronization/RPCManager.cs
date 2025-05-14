using System;
using System.Collections.Generic;
using System.Reflection;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.Synchronization
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

        public static void CallRPC(int netId, string methodName, params object[] parameters)
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
                var rpcAttr = method.GetCustomAttribute<NetRPC>();
                if (rpcAttr != null)
                {
                    if (rpcAttr.ServerOnly && !NetManager.IsHost)
                    {
                        Debug.LogWarning($"RPC {methodName} is server-only");
                        continue;
                    }
                    if (rpcAttr.ClientOnly && NetManager.IsHost)
                    {
                        Debug.LogWarning($"RPC {methodName} is client-only");
                        continue;
                    }
                }

                try
                {
                    var target = _rpcTargets[netId].Find(t => t.GetType() == method.DeclaringType);
                    if (target != null)
                    {
                        Debug.Log("Calling RPC: " + method.Name);
                        method.Invoke(target, parameters);
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
            var message = new RPCMessage(NetManager.ConnectionId(), netId, methodName, parameters);
            NetManager.Send(message);
        }
    }
} 