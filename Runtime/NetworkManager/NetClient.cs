using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public static class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            if (Connection != null) return;
            Messager.RegisterHandler<ConnMessage>(OnConnected);
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
        }
        public static void Disconnect()
        {
            NetManager.Transport.Disconnect();
            Connection = null;
        }

        private static void OnConnected(ConnMessage connection)
        {
            if(Connection != null) Connection = new NetConn(connection.CurrentConnected, false);
            NetManager.allPlayers = connection.AllConnected;
        }

        public static void Send(NetMessage netMessage)
        {
            NetManager.Transport.Send(NetSerializer.Serialize(netMessage));
        }
    }
}
