using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace SimpleNet.Messages
{
    [MessagePackObject]
    public class ReconcileMessage : NetMessage
    {
        [Key(1)] public int ObjectId;
        [Key(2)] public int ComponentId;
        [Key(3)] public DateTime Timestamp;
        [Key(4)] public Dictionary<string, object> Values;

        public ReconcileMessage(){}
        public ReconcileMessage(int objectId, int componentId, DateTime timestamp, Dictionary<string, object> values, int target) : base(new List<int>{target})
        {
            this.ObjectId = objectId;
            this.ComponentId = componentId;
            this.Timestamp = timestamp;
            this.Values = values;
        }
        public override string ToString()
        {
            string changes = Values != null ? string.Join(", ", Values.Select(kv => $"{kv.Key}={kv.Value}")) : "none";
            return $"{base.ToString()} [{Timestamp:HH:mm:ss.fff}] ObjectID:{ObjectId}, ComponentID:{ComponentId}, Changes:{changes}";
        }
    }
}