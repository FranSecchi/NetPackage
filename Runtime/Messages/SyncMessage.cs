using System.Collections.Generic;
using MessagePack;
using System.Linq;

namespace NetPackage.Messages
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

        public override string ToString()
        {
            string changes = changedValues != null ? string.Join(", ", changedValues.Select(kv => $"{kv.Key}={kv.Value}")) : "none";
            return $"{base.ToString()} ObjectID:{ObjectID}, ComponentID:{ComponentId}, Changes:{changes}";
        }
    }
}
