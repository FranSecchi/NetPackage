using System.Collections;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest
{
    public class ClientManagerTests
    {
        private int _port;
        private ITransport host;
        [SetUp]
        public void SetUp()
        {
            NetManager _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            _port = 7777;
            NetManager.Port = _port;
            host = new UDPSolution();
            host.Setup(_port, true);
            
        }
        [UnityTest]
        public IEnumerator TestStartClient()
        {
            yield return new WaitForSeconds(0.5f);
            host.Start();
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(NetClient.Connection != null, "Client did not start correctly");
        }
        
        [UnityTest]
        public IEnumerator TestStopClient()
        {
            yield return new WaitForSeconds(0.5f);
            host.Start();
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);

            NetManager.StopClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsFalse(NetClient.Connection != null, "Client did not stop correctly");
        }
        
        [TearDown]
        public void TearDown()
        {
            NetManager.StopClient();
            host?.Disconnect();
        }
    }
}
