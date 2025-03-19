using UnityEngine;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetClient
    {
        public static NetConn Connection;
        public static void Connect(string address)
        {
            NetManager.Transport.Start();
            Debug.Log("NetClient subscribed");
            NetManager.Transport.OnClientConnected += OnConnected;
            NetManager.Transport.OnClientDisconnected += OnDisconnected;
            NetManager.Transport.Connect(address);
        }

        private static void OnDisconnected(int id)
        {
            if(Connection != null && id == Connection.Id) Connection = null;
        }

        private static void OnConnected(int id)
        {
            Debug.Log("NetClient created: " + id);
            Connection = new NetConn(id);
        }
    }
}
