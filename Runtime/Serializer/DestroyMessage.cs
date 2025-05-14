using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class DestroyMessage : NetMessage
    {
        [Key(0)] public int ObjectId;
        
        public DestroyMessage() { }

        public DestroyMessage(int objectId)
        {
            ObjectId = objectId;
        }
    }
}