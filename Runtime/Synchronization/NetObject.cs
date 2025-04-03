using System;
using System.Collections.Generic;
using UnityEngine;

namespace Synchronization.NetPackage.Runtime.Synchronization
{
    public class NetObject
    {
        public readonly int ObjectId;
        private ObjectState _state;

        public NetObject(int objectId, object obj)
        {
            ObjectId = objectId;
            _state = new ObjectState();
            Register(obj);
            StateManager.Register(this, _state);
        }
        public void Register(object obj)
        {
            _state.Register(obj);
        }
    }
}
