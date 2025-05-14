using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest
{
    public class HostManagerTests
    {
        private int _port;
        private System.Action<int> onClientConnectedHandler;
        [SetUp]
        public void SetUp()
        {
            NetManager _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            NetManager.StartHost();
            
            NetManagerTest _client = new GameObject().AddComponent<NetManagerTest>();
            NetManagerTest.SetTransport(new UDPSolution());
            _client.address = "localhost";
        }
        [UnityTest]
        public IEnumerator TestStartServer()
        {
            yield return new WaitForSeconds(0.2f);
            NetManagerTest.StartClient();
            bool result = false;
            onClientConnectedHandler = i => result = true; 

            ITransport.OnClientConnected += onClientConnectedHandler;
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(result, "Server did not start correctly");
            Assert.IsTrue(NetManager.allPlayers.Count == 2, "Server did not add one player");
            ITransport.OnClientConnected -= onClientConnectedHandler;
        }
        [UnityTest]
        public IEnumerator TestStopServer()
        {
            yield return new WaitForSeconds(0.2f);
            
            NetManagerTest.StartClient();
            yield return new WaitForSeconds(0.2f);

            NetManager.StopHosting();
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(NetManager.allPlayers.Count == 0, "Server did not stop correctly");
        }
        [UnityTest]
        public IEnumerator TestKickPlayer()
        {
            yield return new WaitForSeconds(0.2f);
            
            NetManagerTest.StartClient();
            yield return new WaitForSeconds(0.2f);
            int key = NetManager.allPlayers[1];
            NetHost.Kick(key);
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(NetManager.allPlayers.Count == 1, "Server did not kick correctly " + NetManager.allPlayers.Count);
        }
        [UnityTest]
        public IEnumerator TestMultipleClients()
        {
            yield return new WaitForSeconds(0.2f);
            NetManagerTest.StartClient();
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 3; i++)
            {
                yield return new WaitForSeconds(0.2f);
                ITransport client = new UDPSolution();
                clients.Add(client);
                client.Setup(NetManager.Port, false);
                client.Start();
                yield return new WaitForSeconds(0.2f);
                client.Connect("localhost");
            }
            yield return new WaitForSeconds(0.2f);
            
            
            Assert.IsTrue(NetManager.allPlayers.Count == 5, "Server did not add 5 players");
            Assert.IsTrue(NetManager.allPlayers.Contains(3), "Server did not add correctly");

            foreach (ITransport client in clients)
            {
                client.Stop();
            }
        }
        [TearDown]
        public void TearDown()
        {
            NetManagerTest.StopHosting();
            NetManagerTest.StopClient();
            NetManager.StopHosting();
            NetManager.StopClient();
        }
        
    }
}
