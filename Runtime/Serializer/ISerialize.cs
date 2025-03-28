namespace Serializer.NetPackage.Runtime.Serializer
{
    public interface ISerialize
    {
        public byte[] Serialize<T>(T data);
        public T Deserialize<T>(byte[] data);
    }
}
