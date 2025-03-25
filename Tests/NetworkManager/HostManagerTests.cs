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
        private int _port;
    
        [SetUp]
        public void SetUp()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            _manager.address = "localhost";
            _port = 7777;
            NetManager.Port = _port;
        }
        [UnityTest]
        public IEnumerator TestStartServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(0.5f);
        
            ITransport client = new UDPSolution();
            client.Setup(_port, false);
            client.Start();
            client.Connect("localhost");
            bool result = false;
            ITransport.OnClientConnected += i => result = true; 
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(result, "Server did not start correctly");
        }
        [UnityTest]
        public IEnumerator TestStopServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(0.5f);
            ITransport client = new UDPSolution();
            client.Setup(_port, false);
            client.Start();
            client.Connect("localhost");
            yield return new WaitForSeconds(0.5f);

            _manager.StopHosting();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsEmpty(NetHost.Clients, "Server did not stop correctly");
        }
        [UnityTest]
        public IEnumerator TestKickPlayer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(0.5f);
            ITransport client = new UDPSolution();
            client.Setup(_port, false);
            client.Start();
            client.Connect("localhost");
            yield return new WaitForSeconds(0.5f);

            NetHost.Kick(NetHost.Clients.Keys.Count-1);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsEmpty(NetHost.Clients, "Server did not stop correctly");
        }
        [TearDown]
        public void TearDown()
        {
        }
    }
}
