using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public abstract class NetMessage
    {
        [Key(0)]public List<int> target;


        protected NetMessage(){}
        protected NetMessage(List<int> target = null)
        {
            this.target = target;
        }

        public override string ToString()
        {
            string targetStr = target != null ? string.Join(",", target) : "all";
            return $"{GetType().Name}[Target:{targetStr}]";
        }
    }
}
