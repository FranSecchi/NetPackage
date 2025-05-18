using System.Collections.Generic;
using NetPackage.Synchronization;
using NetPackage.Serializer;
using NetPackage.Messages;
using NetPackage.Transport;

namespace NetPackage.Network
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
