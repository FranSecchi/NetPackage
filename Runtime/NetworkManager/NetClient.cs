using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            ITransport.OnClientConnected += OnConnected;
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
        }
        public static void Disconnect()
        {
            NetManager.Transport.Disconnect();
            ITransport.OnClientConnected -= OnConnected;
            Connection = null;
        }

        private static void OnConnected(int id)
        {
            Connection = new NetConn(id, false);
        }

        public static void Send(NetMessage netMessage)
        {
            NetManager.Transport.Send(NetSerializer.Serialize(netMessage));
        }
    }
}
