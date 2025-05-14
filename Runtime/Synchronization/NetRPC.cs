using System;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NetRPC : Attribute
    {
        public bool ServerOnly { get; set; }
        public bool ClientOnly { get; set; }

        public NetRPC(bool serverOnly = false, bool clientOnly = false)
        {
            ServerOnly = serverOnly;
            ClientOnly = clientOnly;
        }
    }
} 