using System.Collections.Generic;
using MessagePack;

namespace NetPackage.Runtime.Messages
{
    [MessagePackObject]
    public class DestroyMessage : NetMessage
    {
        [Key(1)] public int netObjectId;
        [Key(2)] public int requesterId;

        public DestroyMessage() { }

        public DestroyMessage(int netObjectId, int requesterId, List<int> target = null) : base(target)
        {
            this.netObjectId = netObjectId;
            this.requesterId = requesterId;
        }

        public override string ToString()
        {
            return $"{base.ToString()} NetObjectId:{netObjectId}, Requester:{requesterId}";
        }
    }
}