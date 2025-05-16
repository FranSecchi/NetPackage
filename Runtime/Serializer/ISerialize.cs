using System;

namespace NetPackage.Runtime.Serializer
{
    public interface ISerialize
    {
        public byte[] Serialize<T>(T data);
        public T Deserialize<T>(byte[] data);
        object Deserialize(byte[] data, Type type);
    }
}
