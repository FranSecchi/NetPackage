using System.Collections;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest.NetPackage.Tests.NetworkManager
{
    public class HostManagerTests
    {
        private NetManager _manager;
    
        [SetUp]
        public void SetUp()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
        }
        [UnityTest]
        public IEnumerator TestStartServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(0.5f);
        
            ITransport client = new UDPClient();
            client.Setup(9050, false);
            client.Start();
            client.Connect("localhost");
            bool result = false;
            client.OnClientConnected += i => result = true; 
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(result, "Server did not start correctly");
        }
        [TearDown]
        public void TearDown()
        {
        }
    }
}
