using System.Collections.Generic;

namespace Serializer.NetPackage.Runtime.Serializer
{
    public class SpawnMessage : NetMessage
    {
        
        public SpawnMessage(){}
        public SpawnMessage(List<int> target = null) : base(target)
        {
            
        }
    }
}
