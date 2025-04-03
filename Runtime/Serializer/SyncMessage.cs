using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class SyncMessage : NetMessage
    {
        [Key(1)]public int ObjectID;
        [Key(2)] public Dictionary<string, object> changedValues; // Changed variables

        public SyncMessage(){}
        public SyncMessage(List<int> target, int objectID, Dictionary<string, object> changes) : base(target)
        {
            this.ObjectID = objectID;
            this.changedValues = changes;
        }
    }
}
