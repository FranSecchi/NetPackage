using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public abstract class NetMessage
    {
        [Key(0)]public int ObjectID;
        [Key(1)]public List<int> target;


        protected NetMessage(){}
        protected NetMessage(int objectID, List<int> target = null)
        {
            ObjectID = objectID;
            this.target = target;
        }
    }
}
