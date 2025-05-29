using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MessagePack;
using UnityEngine;

namespace SimpleNet.Messages
{
    /// <summary>
    /// A static utility class that manages message handling and routing for network messages.
    /// Provides functionality to register message handlers and process incoming messages.
    /// </summary>
    public static class Messager
    {
        private static readonly Dictionary<Type, List<Action<NetMessage>>> messageHandlers = new();

        /// <summary>
        /// Registers a handler for a specific type of network message.
        /// </summary>
        /// <typeparam name="T">The type of network message to handle.</typeparam>
        /// <param name="handler">The action to execute when a message of type T is received.</param>
        public static void RegisterHandler<T>(Action<T> handler) where T : NetMessage
        {
            if (!messageHandlers.TryGetValue(typeof(T), out var handlers))
            {
                handlers = new List<Action<NetMessage>>();
                messageHandlers[typeof(T)] = handlers;
            }

            handlers.Add(msg => handler((T)msg));
        }

        /// <summary>
        /// Processes an incoming network message by invoking all registered handlers for its type.
        /// </summary>
        /// <param name="msg">The network message to process.</param>
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

        /// <summary>
        /// Removes all registered message handlers, effectively resetting the message handling system.
        /// </summary>
        public static void ClearHandlers()
        {
            messageHandlers.Clear();
        }
    }
}
