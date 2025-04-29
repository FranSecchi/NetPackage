using System.Collections.Generic;
using Runtime.NetPackage.Runtime.NetworkManager;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.Synchronization
{
    public class NetObject
    {
        public readonly int NetId;
        private int _ownerId;
        private List<object> _behaviours;
        private static int nextId = 0;
        public int OwnerId => _ownerId;

        public NetObject(object behaviour, int ownerId = -1)
        {
            _ownerId = ownerId;
            Register(behaviour);
            NetScene.Register(this);
        }

        public NetObject(int netId, object behaviour, int ownerId = -1)
        {
            NetId = netId;
            _ownerId = ownerId;
            Register(behaviour);
            NetScene.Register(this);
        }

        public void GiveOwner(int ownerId)
        {
            _ownerId = ownerId;
        }

        public void Register(object obj)
        {
            _behaviours.Add(obj);
        }

        public void Unregister(object obj)
        {
            _behaviours.Remove(obj);
        }

        public void Spawn()
        {
            foreach (var behaviour in _behaviours)
            {
                if(behaviour is NetBehaviour netBehaviour)
                    netBehaviour.OnSpawn();
            }
        }
    }
}
