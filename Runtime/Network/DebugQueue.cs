using System.Collections.Generic;
using UnityEngine;
using NetPackage.Messages;

namespace NetPackage.Network
{
    public class DebugQueue
    {
        private static readonly Queue<DebugMessage> messageQueue = new Queue<DebugMessage>();
        private const int MaxMessages = 100;

        public class DebugMessage
        {
            public string Message { get; }
            public MessageType Type { get; }
            public float Timestamp { get; }

            public DebugMessage(string message, MessageType type)
            {
                Message = message;
                Type = type;
                Timestamp = Time.time;
            }
        }

        public enum MessageType
        {
            Info,
            Warning,
            Error,
            Network,
            RPC,
            State
        }

        public static void AddMessage(string message, MessageType type = MessageType.Info)
        {
            lock (messageQueue)
            {
                messageQueue.Enqueue(new DebugMessage(message, type));
                while (messageQueue.Count > MaxMessages)
                {
                    messageQueue.Dequeue();
                }
            }
        }

        public static void AddNetworkMessage(NetMessage message, bool isReceived = true)
        {
            string direction = isReceived ? "Received" : "Sent";
            AddMessage($"[{direction}] {message.GetType().Name}: {message}", MessageType.Network);
        }

        public static void AddRPC(string rpcName, int objectId, int senderId)
        {
            AddMessage($"[RPC] {rpcName} on object {objectId} from {senderId}", MessageType.RPC);
        }

        public static void AddStateChange(int objectId, string stateName)
        {
            AddMessage($"[State] Object {objectId} state changed to {stateName}", MessageType.State);
        }

        public static List<DebugMessage> GetMessages()
        {
            lock (messageQueue)
            {
                return new List<DebugMessage>(messageQueue);
            }
        }

        public static void ClearMessages()
        {
            lock (messageQueue)
            {
                messageQueue.Clear();
            }
        }
    }
} 