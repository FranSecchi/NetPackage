using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class SyncMessage : NetMessage
    {
        [Key(2)] public int ComponentId;
        [Key(3)] public Dictionary<string, object> changedValues; // Changed variables

        public SyncMessage(){}
        public SyncMessage(int objectID, int componentId, Dictionary<string, object> changes, List<int> target = null) : base(objectID, target)
        {
            changedValues = changes;
            ComponentId = componentId;
        }
    }
}
