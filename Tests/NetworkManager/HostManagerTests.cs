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
        private ITransport client;
        private System.Action<int> onClientConnectedHandler;
        [SetUp]
        public void SetUp()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            _manager.address = "localhost";
            _port = 7777;
            NetManager.Port = _port;
            
            client = new UDPSolution();
            client.Setup(_port, false);
            client.Start();
        }
        [UnityTest]
        public IEnumerator TestStartServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(1f);
        
            client.Connect("localhost");
            bool result = false;
            onClientConnectedHandler = i => result = true; 

            // Subscribe to the event
            ITransport.OnClientConnected += onClientConnectedHandler;
            yield return new WaitForSeconds(1f);
            
            Assert.IsTrue(result, "Server did not start correctly");
        }
        [UnityTest]
        public IEnumerator TestStopServer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(1f);
            
            client.Connect("localhost");
            yield return new WaitForSeconds(1f);

            _manager.StopHosting();
            yield return new WaitForSeconds(1f);
            
            Assert.IsEmpty(NetHost.Clients, "Server did not stop correctly");
        }
        [UnityTest]
        public IEnumerator TestKickPlayer()
        {
            _manager.StartHost();
            yield return new WaitForSeconds(1f);
            
            Assert.IsTrue(NetHost.Clients.Keys.Count == 0, "Host did not start correctly: " + NetHost.Clients.Keys);
            client.Connect("localhost");
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(NetHost.Clients.Count == 1, "Host did not save connection correctly: " + NetHost.Clients.Count);
            
            NetHost.Kick(NetHost.Clients[0].Id);
            yield return new WaitForSeconds(1f);
            
            Assert.IsEmpty(NetHost.Clients, "Host did not kick correctly");
        }
        [TearDown]
        public void TearDown()
        {
            ITransport.OnClientConnected -= onClientConnectedHandler;
            _manager.StopHosting();
            client.Disconnect();
        }
        
    }
}
