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
    public class HostManagerTests
    {
        private int _port;
        private System.Action<int> onClientConnectedHandler;
        private GameObject host;
        private GameObject client;
        [SetUp]
        public void SetUp()
        {
            host = new GameObject();
            host.AddComponent<NetManager>();
            NetManager.DebugLog = true;
            NetManager.StartHost();
            
            client = new GameObject();
            client.AddComponent<NetManagerTest>();
            NetManagerTest.DebugLog = true;
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

            NetManager.StopNet();
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
        [UnityTest]
        public IEnumerator TestGetServerInfo()
        {
            NetManager.SetServerName("Net_Server");
            yield return new WaitForSeconds(0.2f);
            
            var serverInfo = NetManager.GetServerInfo();
            Assert.IsNotNull(serverInfo, "Server info should not be null for host");
            Assert.AreEqual(NetManager.MaxPlayers, serverInfo.MaxPlayers, "Server info should match configured max players");
            Assert.AreEqual(NetManager.ServerName, serverInfo.ServerName, "Server info should match configured server name");
        }

        [UnityTest]
        public IEnumerator TestGetConnectionInfo()
        {
            yield return new WaitForSeconds(0.2f);
            NetManagerTest.StartClient();
            yield return new WaitForSeconds(0.2f);
            
            // Test host's own connection info
            var hostConnectionInfo = NetManager.GetConnectionInfo();
            Assert.IsNotNull(hostConnectionInfo, "Host connection info should not be null");
            
            // Test client's connection info from host perspective
            var clientId = NetManager.allPlayers[1];
            var clientConnectionInfo = NetManager.GetConnectionInfo(clientId);
            Assert.IsNotNull(clientConnectionInfo, "Client connection info should not be null");
            Assert.AreEqual(clientId, clientConnectionInfo.Id, "Connection info ID should match client ID");
        }

        [UnityTest]
        public IEnumerator TestGetConnectionState()
        {
            yield return new WaitForSeconds(0.2f);
            NetManagerTest.StartClient();
            yield return new WaitForSeconds(0.2f);
            
            // Test host's own connection state
            var hostConnectionState = NetManager.GetConnectionState();
            Assert.IsNotNull(hostConnectionState, "Host connection state should not be null");
            Assert.AreEqual(ConnectionState.Connected, hostConnectionState, "Host should be in Connected state");
            
            // Test client's connection state from host perspective
            var clientId = NetManager.allPlayers[1];
            var clientConnectionState = NetManager.GetConnectionState(clientId);
            Assert.IsNotNull(clientConnectionState, "Client connection state should not be null");
            Assert.AreEqual(ConnectionState.Connected, clientConnectionState, "Client should be in Connected state");
        }
        [TearDown]
        public void TearDown()
        {
            NetManagerTest.StopNet();
            NetManager.StopNet();
            GameObject.DestroyImmediate(host);
            GameObject.DestroyImmediate(client);
        }
    }
}
