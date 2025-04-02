namespace Serializer.NetPackage.Runtime.Serializer
{
    public static class NetSerializer
    {
        public static ISerialize _Serializer = new MPSerializer();

        public static byte[] Serialize<T>(T data) where T : NetMessage
        {
            return _Serializer.Serialize(data);
        }

        public static NetMessage Deserialize<T>(byte[] data) where T : NetMessage
        {
            return _Serializer.Deserialize<NetMessage>(data);
        }
    }
}
