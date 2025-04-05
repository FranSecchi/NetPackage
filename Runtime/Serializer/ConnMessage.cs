using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Serializer.NetPackage.Runtime.Serializer;
using UnityEngine;

namespace Serializer
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
    }
}
