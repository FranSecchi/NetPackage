using System.Collections.Generic;
using System.Linq;
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
        public override string ToString()
        {
            string changes = Values != null ? string.Join(", ", Values.Select(kv => $"{kv.Key}={kv.Value}")) : "none";
            return $"{base.ToString()} [{Timestamp}] ObjectID:{ObjectId}, ComponentID:{ComponentId}, Changes:{changes}";
        }
    }
}