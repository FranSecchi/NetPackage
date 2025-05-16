using System.Collections.Generic;
using System.Linq;
using NetPackage.Runtime.Messages;
using NetPackage.Runtime.NetworkManager;
using UnityEngine;

namespace NetPackage.Runtime.Synchronization
{
    public class NetObject
    {
        public readonly int NetId;
        private int _ownerId;
        public List<NetBehaviour> _behaviours;
        public int OwnerId
        {
            get => _ownerId;
            set => _ownerId = value;
        }

        // Changed logic: object is owned if the current connection is the owner
        public bool Owned => _ownerId == NetManager.ConnectionId();

        public NetObject(int netId, NetBehaviour behaviour, int ownerId = -1)
        {
            NetId = netId;
            _ownerId = ownerId == -1 ? NetManager.ConnectionId() : ownerId;
            _behaviours = behaviour.GetComponents<NetBehaviour>().ToList();

            foreach (var b in _behaviours)
            {
                Register(b);
            }
            
            // Update NetBehaviour ownership state
            UpdateBehaviourOwnership();
        }

        public void GiveOwner(int ownerId)
        {
            if (_ownerId == ownerId) return;
            if (!NetManager.allPlayers.Contains(ownerId)) return;
            _ownerId = ownerId;
            UpdateBehaviourOwnership();
            
            if (NetManager.IsHost)
            {
                NetMessage msg = new OwnershipMessage(NetId, ownerId);
                Debug.Log("Sent ownership message");
                NetManager.Send(msg);
            }
        }

        private void UpdateBehaviourOwnership()
        {
            foreach (var behaviour in _behaviours)
            {
                behaviour.isOwned = Owned;
            }
        }

        public void Register(NetBehaviour obj)
        {
            if (!_behaviours.Contains(obj))
            {
                _behaviours.Add(obj);
            }
            obj.SetNetObject(this);
            obj.isOwned = Owned;
        }

        public void Destroy()
        {
            GameObject.Destroy(_behaviours[0].gameObject);
        }

        public void Enable()
        {
            if(!_behaviours[0].gameObject.activeSelf) _behaviours[0].gameObject.SetActive(true);
        }
    }
}
