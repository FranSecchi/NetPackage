using System.Collections;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest.NetPackage.Tests.NetworkManager
{
    public class ClientManagerTests : MonoBehaviour
    {
        private NetManager _manager;
    
        [SetUp]
        private void Setup()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            _manager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            
        }
        [UnityTest]
        public IEnumerator TestStartClient()
        {
            ITransport host = new UDPHost();
            // host.Setup(9050);
            host.Start();
            host.Connect("localhost", 9050);
            yield return new WaitForSeconds(0.5f);

            _manager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(_manager.GetClientsCount() == 1, "Client did not start correctly");
        }
    }
}
