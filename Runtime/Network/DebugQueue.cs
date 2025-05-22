using System;
using System.Collections.Generic;
using NetPackage.Messages;

namespace NetPackage.Network
{
    public class DebugQueue
    {
        private static readonly Queue<DebugMessage> messageQueue = new Queue<DebugMessage>();
        private const int MaxMessages = 100;
        private static bool[] enabledMessageTypes;

        public class DebugMessage
        {
            public string Message { get; }
            public MessageType Type { get; }
            public DateTime Timestamp { get; }

            public DebugMessage(string message, MessageType type)
            {
                Message = message;
                Type = type;
                Timestamp = DateTime.Now;
            }
        }

        public enum MessageType
        {
            Info,
            Warning,
            Error,
            Network,
            RPC,
            State,
            Rollback
        }

        static DebugQueue()
        {
            enabledMessageTypes = new bool[Enum.GetValues(typeof(MessageType)).Length];
            for (int i = 0; i < enabledMessageTypes.Length; i++)
            {
                enabledMessageTypes[i] = true;
            }
        }

        public static void SetMessageTypeEnabled(MessageType type, bool enabled)
        {
            enabledMessageTypes[(int)type] = enabled;
        }

        public static bool IsMessageTypeEnabled(MessageType type)
        {
            return enabledMessageTypes[(int)type];
        }

        public static void AddMessage(string message, MessageType type = MessageType.Info)
        {
            if (!IsMessageTypeEnabled(type))
                return;

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
            if (!IsMessageTypeEnabled(MessageType.Network))
                return;

            string direction = isReceived ? "Received" : "Sent";
            AddMessage($"[{direction}] {message.GetType().Name}: {message}", MessageType.Network);
        }

        public static void AddRPC(string rpcName, int objectId, int senderId)
        {
            if (!IsMessageTypeEnabled(MessageType.RPC))
                return;

            AddMessage($"[RPC] {rpcName} on object {objectId} from {senderId}", MessageType.RPC);
        }

        public static void AddStateChange(int objectId, int componentId, string stateName, object change)
        {
            if (!IsMessageTypeEnabled(MessageType.State))
                return;

            AddMessage($"[State] Object {objectId}, component {componentId} changed {stateName} to {change}", MessageType.State);
        }

        public static void AddRollback(int objectId, float targetTime, string reason)
        {
            if (!IsMessageTypeEnabled(MessageType.Rollback))
                return;

            AddMessage($"[Rollback] Object {objectId} rolling back to time {targetTime:F3}s. Reason: {reason}", MessageType.Rollback);
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