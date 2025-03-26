using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetConn
    {
        public int Id { get; private set; }
        public bool IsHost { get; private set; }
        private ITransport _transport = NetManager.Transport;
        public NetConn(int id, bool isHost)
        {
            Id = id;
            IsHost = isHost;
        }

        public void Disconnect()
        {
            _transport?.Kick(Id);
        }
    }
}
