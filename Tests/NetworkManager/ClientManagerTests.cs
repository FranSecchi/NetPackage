using System.Collections;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest.NetPackage.Tests.NetworkManager
{
    public class ClientManagerTests
    {
        private NetManager _manager;
        private int _port;
        [SetUp]
        public void SetUp()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            _port = 7777;
            NetManager.Port = _port;
            
        }
        [UnityTest]
        public IEnumerator TestStartClient()
        {
            ITransport host = new UDPSolution();
            host.Setup(_port, true);
            host.Start();
            yield return new WaitForSeconds(0.5f);

            _manager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(NetClient.Connection != null, "Client did not start correctly");
        }
        
        [UnityTest]
        public IEnumerator TestStopClient()
        {
            ITransport host = new UDPSolution();
            host.Setup(_port, true);
            host.Start();
            yield return new WaitForSeconds(0.5f);

            _manager.StartClient();
            yield return new WaitForSeconds(0.5f);

            _manager.StopClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsFalse(NetClient.Connection != null, "Client did not stop correctly");
        }
        
        [TearDown]
        public void TearDown()
        {
        }
    }
}
