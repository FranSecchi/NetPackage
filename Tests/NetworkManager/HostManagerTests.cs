using System.Collections;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest
{
    public class HostManagerTests
    {
        private int _port;
        private ITransport client;
        private System.Action<int> onClientConnectedHandler;
        [SetUp]
        public void SetUp()
        {
            NetManager _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            NetManager.StartHost();
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
        }
        [UnityTest]
        public IEnumerator TestStartServer()
        {
            yield return new WaitForSeconds(0.5f);
        
            client.Connect("localhost");
            bool result = false;
            onClientConnectedHandler = i => result = true; 

            ITransport.OnClientConnected += onClientConnectedHandler;
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(result, "Server did not start correctly");
            ITransport.OnClientConnected -= onClientConnectedHandler;
        }
        [UnityTest]
        public IEnumerator TestStopServer()
        {
            yield return new WaitForSeconds(0.5f);
            
            client.Connect("localhost");
            yield return new WaitForSeconds(0.5f);

            NetManager.StopHosting();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsEmpty(NetHost.Clients, "Server did not stop correctly");
        }
        [UnityTest]
        public IEnumerator TestKickPlayer()
        {
            yield return new WaitForSeconds(0.5f);
            
            client.Connect("localhost");
            yield return new WaitForSeconds(0.5f);
            int key = NetHost.Clients[0].Id;
            NetHost.Kick(key);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsEmpty(NetHost.Clients, "Host did not kick correctly" + NetHost.Clients.Count);
        }
        [TearDown]
        public void TearDown()
        {
            Debug.Log("Tearing down");
            NetManager.StopHosting();
            client?.Disconnect();
        }
        
    }
}
