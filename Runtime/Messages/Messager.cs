using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetPackage.Messages
{
    public static class Messager
    {
        private static readonly Dictionary<Type, List<Action<NetMessage>>> messageHandlers = new();

        public static void RegisterHandler<T>(Action<T> handler) where T : NetMessage
        {
            if (!messageHandlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Action<NetMessage>>();
                messageHandlers[typeof(T)] = handlers;
            }

            handlers.Add(msg => handler((T)msg));
        }

        public static void HandleMessage(NetMessage msg)
        {
            if (messageHandlers.TryGetValue(msg.GetType(), out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler.Invoke(msg);
                }
            }
            else
            {
                Debug.LogError($"No handler found for message type: {msg.GetType()}");
            }

        }

        public static void ClearHandlers()
        {
            messageHandlers.Clear();
        }
    }
}
