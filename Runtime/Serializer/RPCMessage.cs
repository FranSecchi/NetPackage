using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class RPCMessage : NetMessage
    {
        [Key(1)] public int SenderID { get; private set; }
        [Key(2)] public int ObjectId;
        [Key(3)] public string MethodName;
        [Key(4)] public object[] Parameters;

        public RPCMessage() { }
        public RPCMessage(int SenderID, int objectId, string methodName, List<int> target = null, params object[] parameters) : base(target)
        {
            this.SenderID = SenderID;
            this.ObjectId = objectId;
            this.MethodName = methodName;
            this.Parameters = parameters;
        }
    }
}