using Transport.NetPackage.Runtime.Transport;
using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            ITransport.OnClientConnected += OnConnected;
            NetManager.Transport.Start();
            NetManager.Transport.Connect(address);
        }
        public static void Disconnect()
        {
            NetManager.Transport.Disconnect();
            Connection = null;
        }

        private static void OnConnected(int id)
        {
            Connection = new NetConn(id);
        }

    }
}
