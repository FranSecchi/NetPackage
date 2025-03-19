using UnityEngine;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine.UIElements;

namespace NetworkManager.NetPackage.Runtime.NetworkManager
{
    public class NetManager : MonoBehaviour
    {
        public string address = "localhost";
        private ITransport _transport;
        private int Port { get; set; } = 9050;

        public void SetTransport(ITransport transport)
        {
            _transport = transport;
        }


        public void StartHost()
        {
            _transport.Setup(Port, true);
            _transport.Start();
        }
        public void StartClient()
        {
            _transport.Setup(Port, false);
            _transport.Start();
        }
        public int GetClientsCount()
        {
            throw new System.NotImplementedException();
        }
    }
}
