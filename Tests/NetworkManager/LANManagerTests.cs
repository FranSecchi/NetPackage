using System.Collections;
using NUnit.Framework;
using NetPackage.Runtime.NetworkManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetworkManagerTest
{
    public class LANManagerTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            NetManager _manager = new GameObject().AddComponent<NetManager>();
            NetManager.UseLan = true;
            NetManager.StartHost();
            
            yield return new WaitForSeconds(3f);
            
            NetManagerTest _host = new GameObject().AddComponent<NetManagerTest>();
            NetManagerTest.UseLan = true;
            NetManagerTest.DebugLog = true;
            NetManagerTest.StartClient();
            yield return new WaitForSeconds(3f);
        }

        [UnityTest]
        public IEnumerator TestLANClientConnection()
        {
            var discoveredServers = NetManagerTest.GetDiscoveredServers();
            Assert.IsTrue(discoveredServers.Count > 0, "No LAN servers were discovered");
            NetManagerTest.ConnectTo(discoveredServers[0].EndPoint);
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(NetManagerTest.allPlayers.Count == 2, "LAN client did not connect to server");
        }

        [TearDown]
        public void TearDown()
        {
            NetManager.StopNet();
            NetManagerTest.StopNet();
            NetManager.UseLan = false;
            NetManagerTest.UseLan = false;
        }
    }
} 