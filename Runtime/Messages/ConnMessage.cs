using System.Collections.Generic;
using MessagePack;
using NetPackage.Transport;

namespace NetPackage.Messages
{
    [MessagePackObject]
    public class ConnMessage : NetMessage
    {
        [Key(1)]public int CurrentConnected;
        [Key(2)]public List<int> AllConnected;
        [Key(3)]public ServerInfo ServerInfo;
        
        public ConnMessage(){}

        public ConnMessage(int currentConnected, List<int> allConnected, ServerInfo serverInfo, List<int> target = null) : base(target)
        {
            this.CurrentConnected = currentConnected;
            this.AllConnected = allConnected;
            this.ServerInfo = serverInfo;
        }

        public override string ToString()
        {
            string allConnStr = AllConnected != null ? string.Join(",", AllConnected) : "none";
            return $"{base.ToString()} Current:{CurrentConnected}, AllConnected:[{allConnStr}], ServerInfo:{ServerInfo}";
        }
    }
}
