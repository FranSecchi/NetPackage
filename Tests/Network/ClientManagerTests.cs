using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NetPackage.Network;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest
{
    public class ClientManagerTests
    {
        private int _port;
        [SetUp]
        public void SetUp()
        {
            NetManager _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            
            NetManagerTest _host = new GameObject().AddComponent<NetManagerTest>();
            NetManagerTest.SetTransport(new UDPSolution());
            NetManagerTest.StartHost();
            _host.address = "localhost";
        }
        [UnityTest]
        public IEnumerator TestStartClient()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(NetManager.allPlayers.Count == 2, "Client did not start correctly");
        }
        
        [UnityTest]
        public IEnumerator TestStopClient()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);

            NetManager.StopNet();
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(NetManager.allPlayers.Count == 0, "Client did not stop correctly");
            Assert.IsTrue(NetManagerTest.allPlayers.Count == 1, "Client did not start correctly");
        }
        
        [UnityTest]
        public IEnumerator TestMultipleClients()
        {
            yield return new WaitForSeconds(0.5f);
            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 3; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(NetManager.Port, false);
                client.Start();
                clients.Add(client);
                yield return new WaitForSeconds(0.2f);
                client.Connect("localhost");
                yield return new WaitForSeconds(0.2f);
            }
            
            Debug.Log(NetManager.allPlayers.Count);
            Assert.IsTrue(NetManager.allPlayers.Count == 5, "Client did not add 5 players");
            Assert.IsTrue(NetManager.allPlayers.Contains(3), "Client did not add correctly");
            foreach (ITransport client in clients)
            {
                client.Stop();
            }
        }

        [UnityTest]
        public IEnumerator TestGetServerInfo()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            var serverInfo = NetManager.GetServerInfo();
            Assert.IsNotNull(serverInfo, "Server info should not be null after connecting");
            Assert.IsTrue(serverInfo.MaxPlayers > 0, "Server info should have valid max players");
        }

        [UnityTest]
        public IEnumerator TestGetConnectionInfo()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            var connectionInfo = NetManager.GetConnectionInfo();
            Assert.IsNotNull(connectionInfo, "Connection info should not be null after connecting");
            Assert.IsTrue(connectionInfo.Id == 0, "Connection info should have valid connection ID");
        }

        [UnityTest]
        public IEnumerator TestGetConnectionState()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            var connectionState = NetManager.GetConnectionState();
            Assert.IsNotNull(connectionState, "Connection state should not be null after connecting");
            Assert.AreEqual(ConnectionState.Connected, connectionState, "Connection state should be Connected");
        }

        [TearDown]
        public void TearDown()
        {
            NetManager.StopNet();
            NetManagerTest.StopNet();
        }
    }
}
