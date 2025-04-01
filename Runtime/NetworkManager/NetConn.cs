using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetConn
    {
        public int Id { get; private set; }
        public bool IsHost { get; private set; }
        private readonly ITransport _transport;
        public NetConn(int id, bool isHost)
        {
            Id = id;
            IsHost = isHost;
            _transport = NetManager.Transport;
        }

        public void Disconnect()
        {
            _transport?.Kick(Id);
        }
        public void Send(NetMessage netMessage)
        {
            _transport.SendTo(Id,NetSerializer.Serialize(netMessage));
        }
    }
}
