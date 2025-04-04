using System.Collections.Generic;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using Serializer;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;

namespace Runtime.NetPackage.Runtime.NetworkManager
{
    public class NetManager : MonoBehaviour
    {
        private static NetManager _manager;
        public static ITransport Transport;
        public static int Port = 9050;
        public static List<int> allPlayers;
        private static bool IsHost = false;
        
        public string address = "localhost";
        
        public static void SetTransport(ITransport transport)
        {
            Transport = transport;
        }

        private void Awake()
        {
            if (_manager != null)
                Destroy(this);
            else _manager = this;
            Transport ??= new UDPSolution();
            allPlayers = new List<int>();
            DontDestroyOnLoad(this);
        }
        public static void StartHost()
        {
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, true);
            IsHost = true;
            NetHost.StartHost();
        }
        public static void StopHosting()
        {
            if (!IsHost) return;
            NetHost.Stop();
            StopNet();
        }
        public static void StartClient()
        {
            ITransport.OnDataReceived += Receive;
            Transport.Setup(Port, false);
            IsHost = false;
            NetClient.Connect(_manager.address);
        }
        public static void StopClient()
        {
            if (IsHost) return;
            NetClient.Disconnect();
            StopNet();
        }

        private static void StopNet()
        {
            allPlayers.Clear();
            Messager.ClearHandlers();
            ITransport.OnDataReceived -= Receive;
        }
        public int ConnectionId()
        {
            if (!IsHost) return NetClient.Connection.Id;
            return 0;
        }
        public static void Send(NetMessage netMessage)
        {
            if(IsHost)
                NetHost.Send(netMessage);
            else NetClient.Send(netMessage);
        }
        private static void Receive(int id)
        {
            byte[] data = Transport.Receive();
            
            if (data != null && data.Length != 0)
            {
                Messager.HandleMessage(data);
            }
        }
    }
}
