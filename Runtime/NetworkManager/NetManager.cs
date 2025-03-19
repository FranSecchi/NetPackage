using UnityEngine;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine.UIElements;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetManager : MonoBehaviour
    {
        private static NetManager _manager;
        public static ITransport Transport;
        public static int Port = 9050;
        
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
            DontDestroyOnLoad(this);
        }
        public void StartHost()
        {
            Transport.Setup(Port, true);
            NetHost.StartHost();
        }
        public void StartClient()
        {
            Transport.Setup(Port, false);
            NetClient.Connect(address);
        }
    }
}
