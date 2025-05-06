using System.Collections.Generic;
using MessagePack;

namespace Serializer.NetPackage.Runtime.Serializer
{
    [MessagePackObject]
    public class ConnMessage : NetMessage
    {
        [Key(1)]public int CurrentConnected;
        [Key(2)]public List<int> AllConnected;
        
        public ConnMessage(){}

        public ConnMessage(int currentConnected, List<int> allConnected, List<int> target = null) : base(target)
        {
            this.CurrentConnected = currentConnected;
            this.AllConnected = allConnected;
        }

        public override string ToString()
        {
            string allConnStr = AllConnected != null ? string.Join(",", AllConnected) : "none";
            return $"{base.ToString()} Current:{CurrentConnected}, AllConnected:[{allConnStr}]";
        }
    }
}
