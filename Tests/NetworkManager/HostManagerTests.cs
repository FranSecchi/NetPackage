using System.Collections;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest.NetPackage.Tests.NetworkManager
{
    public class HostManagerTests : MonoBehaviour
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
        public IEnumerator TestStartServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(0.5f);
        
            ITransport client = new UDPClient();
            // client.Setup(9050);
            client.Start();
            client.Connect("localhost", 9050);
            yield return new WaitForSeconds(0.5f);
        
            Assert.IsTrue(_manager.GetClientsCount() == 1, "Server did not start correctly");
        }
    }
}
