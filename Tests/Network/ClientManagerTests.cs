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
        private GameObject host;
        private GameObject client;
        [SetUp]
        public void SetUp()
        {
            client = new GameObject();
            client.AddComponent<NetManager>();
            NetManager.DebugLog = true;
            
            host = new GameObject();
            host.AddComponent<NetManagerTest>();
            NetManagerTest.DebugLog = true;
            NetManagerTest.StartHost();
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
            
            var clientInfo = NetManager.GetServerInfo();
            var serverInfo = NetManagerTest.GetServerInfo();
            Assert.IsNotNull(serverInfo, "Server info should not be null after connecting");
            Assert.IsNotNull(serverInfo.Address, "Server end should not be null after connecting");
            Assert.IsNotNull(clientInfo.Address, "Client end should not be null after connecting");
            Assert.AreEqual(clientInfo.Address, serverInfo.Address, $"EndPoint not matching {clientInfo.Address} | {serverInfo.Address}");
            Assert.AreEqual(clientInfo.MaxPlayers, serverInfo.MaxPlayers, $"Max players not matching {clientInfo.MaxPlayers} | {serverInfo.MaxPlayers}");
            Assert.AreEqual(clientInfo.ServerName, serverInfo.ServerName, $"Server name not matching {clientInfo.ServerName} | {serverInfo.ServerName}");
        }

        [UnityTest]
        public IEnumerator TestGetConnectionInfo()
        {
            yield return new WaitForSeconds(0.5f);

            NetManager.StartClient();
            yield return new WaitForSeconds(0.5f);
            
            var connectionInfo = NetManager.GetConnectionInfo();
            Debug.Log(connectionInfo);
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
            var connectionInfo = NetManager.GetConnectionInfo();
            Debug.Log(connectionInfo);
            Assert.IsNotNull(connectionState, "Connection state should not be null after connecting");
            Assert.AreEqual(ConnectionState.Connected, connectionState, "Connection state should be Connected");
        }

        [TearDown]
        public void TearDown()
        {
            NetManager.StopNet();
            NetManagerTest.StopNet();
            GameObject.DestroyImmediate(host);
            GameObject.DestroyImmediate(client);
        }
    }
}
