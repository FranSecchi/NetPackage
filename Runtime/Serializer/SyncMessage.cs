using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class SyncMessage : NetMessage
    {
        [Key(2)] public Dictionary<string, object> changedValues; // Changed variables

        public SyncMessage(){}
        public SyncMessage(List<int> target, int objectID, Dictionary<string, object> changes) : base(objectID, target)
        {
            this.changedValues = changes;
        }
    }
}
