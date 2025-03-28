using MessagePack;
using UnityEngine;

namespace Serializer.NetPackage.Runtime.Serializer
{
    public class MPSerializer : ISerialize
    {
        public byte[] Serialize<T>(T data)
        {
            return MessagePackSerializer.Serialize(data);
        }

        // Generic method to deserialize any object
        public T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
