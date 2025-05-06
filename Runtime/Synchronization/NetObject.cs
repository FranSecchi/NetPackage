using System.Collections.Generic;
using System.Linq;
using Runtime.NetPackage.Runtime.NetworkManager;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    public class NetObject
    {
        public readonly int NetId;
        private int _ownerId;
        private List<NetBehaviour> _behaviours;
        private int OwnerId => _ownerId;
        public bool Owned => _ownerId != NetManager.ConnectionId();

        public NetObject(int netId, NetBehaviour behaviour, int ownerId = -1)
        {
            NetId = netId;
            _ownerId = ownerId;
            _behaviours = behaviour.GetComponents<NetBehaviour>().ToList();

            foreach (var b in _behaviours)
            {
                Register(b);
            }
        }

        public void GiveOwner(int ownerId)
        {
            _ownerId = ownerId;
        }

        public void Register(NetBehaviour obj)
        {
            obj.SetNetObject(this);
        }
    }
}
