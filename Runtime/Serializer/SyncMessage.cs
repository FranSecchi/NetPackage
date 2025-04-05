using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class SyncMessage : NetMessage
    {
        [Key(1)]public int ObjectID;
        [Key(2)]public int ComponentId;
        [Key(3)] public Dictionary<string, object> changedValues; // Changed variables

        public SyncMessage(){}
        public SyncMessage(int objectID, int componentID, Dictionary<string, object> changes, List<int> target = null) : base(target)
        {
            this.ObjectID = objectID;
            this.changedValues = changes;
            ComponentId = componentID;
        }
    }
}
