using System.Collections.Generic;
using MessagePack;
using NetPackage.Synchronization;

namespace NetPackage.Messages
{
    [MessagePackObject]
    public class ReconcileMessage : NetMessage
    {
        [Key(1)] public int ObjectId;
        [Key(2)] public int ComponentId;
        [Key(3)] public float Timestamp;
        [Key(4)] public Dictionary<string, object> Values;

        public ReconcileMessage(){}
        public ReconcileMessage(int objectId, int componentId, float timestamp, Dictionary<string, object> values, int target) : base(new List<int>{target})
        {
            this.ObjectId = objectId;
            this.ComponentId = componentId;
            this.Timestamp = timestamp;
            this.Values = values;
        }
    }
}