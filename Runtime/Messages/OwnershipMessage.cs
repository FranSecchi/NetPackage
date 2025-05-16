using System.Collections.Generic;
using MessagePack;

namespace NetPackage.Runtime.Messages
{
    [MessagePackObject]
    public class OwnershipMessage : NetMessage
    {
        [Key(1)]public int netObjectId;
        [Key(2)]public int newOwnerId;

        public OwnershipMessage(){}
        
        public OwnershipMessage(int netObjectId, int newOwnerId, List<int> target = null) : base(target)
        {
            this.netObjectId = netObjectId;
            this.newOwnerId = newOwnerId;
        }

        public override string ToString()
        {
            return $"{base.ToString()} ObjectID:{netObjectId}, NewOwner:{newOwnerId}";
        }
    }
} 