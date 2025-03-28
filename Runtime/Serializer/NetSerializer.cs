namespace Serializer.NetPackage.Runtime.Serializer
{
    public static class NetSerializer
    {
        public static ISerialize _Serializer;
        public static byte[] Serialize<T>(T data)
        {
            return _Serializer.Serialize(data);
        }

        // Generic method to deserialize any object
        public static T Deserialize<T>(byte[] data)
        {
            return _Serializer.Deserialize<T>(data);
        }
    }
}
