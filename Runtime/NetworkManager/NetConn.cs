using System.Collections.Generic;
using Runtime.NetPackage.Runtime.Synchronization;
using NetPackage.Runtime.Serializer;
using NetPackage.Runtime.Messages;
using Transport.NetPackage.Runtime.Transport;

namespace Runtime.NetPackage.Runtime.NetworkManager
{
    public class NetConn
    {
        public int Id { get; private set; }
        public bool IsHost { get; private set; }
        public List<int> Objects;
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
            byte[] data = NetSerializer.Serialize(netMessage);
            if(IsHost)
                _transport.SendTo(Id, data);
            else _transport.Send(data);
        }
        public void Own(NetObject netObject)
        {
            Objects.Add(netObject.NetId);
            netObject.GiveOwner(Id);
        }
        public void Disown(NetObject netObject)
        {
            Objects.Remove(netObject.NetId);
        }
    }
}
