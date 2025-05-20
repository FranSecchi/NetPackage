using System.Collections.Generic;
using MessagePack;

namespace NetPackage.Messages
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

        public override string ToString()
        {
            return $"RPCMessage(SenderID: {SenderID}, ObjectId: {ObjectId}, Method: {MethodName}, Parameters: {string.Join(", ", Parameters ?? new object[0])})";
        }
    }
}