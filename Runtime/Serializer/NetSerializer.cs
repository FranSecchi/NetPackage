using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    public class NetSerializer 
    {
        public static byte[] Serialize<T>(T data)
        {
            return MessagePackSerializer.Serialize(data);
        }

        // Generic method to deserialize any object
        public static T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
