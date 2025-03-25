using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetConn
    {
        public readonly int Id;
        private ITransport _transport = NetManager.Transport;
        public NetConn(int id)
        {
            Id = id;
        }

        public void Disconnect()
        {
            _transport?.Kick(Id);
        }
    }
}
