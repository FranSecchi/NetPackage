using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using UnityEngine;

namespace Serializer.NetPackage.Runtime.Serializer
{
    public static class Messager
    {
        private static readonly Dictionary<Type, Action<NetMessage>> messageHandlers = new();

        public static void RegisterHandler<T>(Action<T> handler) where T : NetMessage
        {
            Action<NetMessage> wrapper = (NetMessage msg) =>
            {
                handler((T)msg);
            };

            messageHandlers[typeof(T)] = wrapper;
        }

        public static void HandleMessage(byte[] rawData)
        {
            try
            {
                NetMessage msg = NetSerializer.Deserialize<NetMessage>(rawData);
                if (messageHandlers.TryGetValue(msg.GetType(), out var handler))
                {
                    handler.Invoke(msg);
                }
                else
                {
                    Debug.LogError($"No handler found for message type: {msg.GetType()}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Deserialization error: {e.Message}");
            }
        }

        public static void ClearHandlers()
        {
            messageHandlers.Clear();
        }
    }
}
