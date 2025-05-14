using System;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    public enum Direction
    {
        ClientToServer,
        ServerToClient,
        Bidirectional
    }
    public enum Send
    {
        All,
        Specific,
        Others
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class NetRPC : Attribute
    {
        public Direction Direction { get; set; }
        public Send TargetMode { get; set; }

        public NetRPC(Direction direction = Direction.Bidirectional, Send targetMode = Send.All)
        {
            Direction = direction;
            TargetMode = targetMode;
        }
    }
} 