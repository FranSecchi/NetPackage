using System.Collections;
using System.Collections.Generic;
using System.Net;
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
        public IEnumerator TestLANClientConnection()
        {
            // Setup host with LAN enabled
            NetManagerTest.UseLan = true;
            NetManagerTest.DebugLog = true;
            NetManagerTest.StartHost();

            yield return new WaitForSeconds(2f);

            // Setup client with LAN enabled
            NetManager.UseLan = true;
            NetManager.DebugLog = true;
            NetManager.StartClient();

            yield return new WaitForSeconds(2f);

            var discoveredServers = NetManager.GetDiscoveredServers();
            Assert.IsTrue(discoveredServers.Count > 0, "No LAN servers were discovered");
            NetManager.ConnectTo(discoveredServers[0]);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(NetManager.allPlayers.Count == 2, "LAN client did not connect to server");
        }

        // [UnityTest]
        // public IEnumerator TestLANClientReconnection()
        // {
        //     // Setup host with LAN enabled
        //     NetManagerTest.UseLan = true;
        //     NetManagerTest.DebugLog = true;
        //     NetManagerTest.StartHost();
        //
        //     yield return new WaitForSeconds(2f);
        //
        //     // Setup client with LAN enabled
        //     NetManager.UseLan = true;
        //     NetManager.DebugLog = true;
        //     NetManager.StartClient();
        //
        //     yield return new WaitForSeconds(2f);
        //     NetManager.ConnectTo(NetManager.GetDiscoveredServers()[0]);
        //     yield return new WaitForSeconds(0.2f);
        //
        //     // Disconnect and reconnect
        //     NetManager.StopNet();
        //     yield return new WaitForSeconds(0.5f);
        //
        //     NetManager.StartClient();
        //     yield return new WaitForSeconds(0.5f);
        //
        //     // Verify reconnection
        //     Assert.IsTrue(NetManager.allPlayers.Count == 2, "LAN client did not reconnect to server");
        // }

        [TearDown]
        public void TearDown()
        {
            NetManagerTest.StopNet();
            NetManager.StopNet();
        }
    }
}
