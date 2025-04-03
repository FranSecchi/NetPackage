using NetworkManager.NetPackage.Runtime.NetworkManager;
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
        
        public string address = "localhost";
        private bool _isHost = false;
        
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
            ITransport.OnDataReceived += Receive;
            DontDestroyOnLoad(this);
        }
        public static void StartHost()
        {
            Transport.Setup(Port, true);
            _manager._isHost = true;
            NetHost.StartHost();
        }
        public static void StopHosting()
        {
            if(_manager._isHost) NetHost.Stop();
        }
        public static void StartClient()
        {
            Transport.Setup(Port, false);
            _manager._isHost = false;
            NetClient.Connect(_manager.address);
        }
        public static void StopClient()
        {
            if(!_manager._isHost) NetClient.Disconnect();
        }

        public static void Send(NetMessage netMessage)
        {
            if(_manager._isHost)
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
