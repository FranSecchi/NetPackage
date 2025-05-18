using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;
namespace TransportTest
{
    public class UDPLanTest
    {
        private const int Port = 7777;
        private List<ITransport> _servers = new List<ITransport>();
        private ITransport _client;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            for (int i = 0; i < 3; i++)
            {
                ITransport server = new UDPSolution();
                server.Setup(Port + i, true, useDebug:true);
                server.Start();
                server.SetServerInfo(new ServerInfo(){ServerName = "Name"});
                server.BroadcastServerInfo();
                _servers.Add(server);
                yield return new WaitForSeconds(1f);
            }
            
            _client = new UDPSolution();
            _client.Setup(Port, false, useDebug:true);
            _client.Start();
            _client.StartServerDiscovery();
            yield return new WaitForSeconds(2f);
        }

        [UnityTest]
        public IEnumerator TestDiscoverServer()
        {
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotEmpty(_client.GetDiscoveredServers(), "GetDiscoveredServers failed");
        }

        [UnityTest] 
        public IEnumerator TestDiscoverMultipleServers()
        {
            yield return new WaitForSeconds(0.2f);
            List<ServerInfo> discoveredServers = _client.GetDiscoveredServers();

            Debug.Log($"Discovered servers: {string.Join(", ", discoveredServers)}");
            Assert.GreaterOrEqual(discoveredServers.Count, 2, "Expected multiple servers, but found less.");
            Assert.AreEqual(discoveredServers.Count, new HashSet<ServerInfo>(discoveredServers).Count, "Duplicate servers detected.");
        }

        [UnityTest]
        public IEnumerator TestServerTimeout()
        {
            // Wait for initial server discovery
            yield return new WaitForSeconds(2f);
            List<ServerInfo> initialServers = _client.GetDiscoveredServers();
            Assert.IsNotEmpty(initialServers, "No servers discovered initially");
            
            // Stop one server
            var serverToStop = _servers[0];
            serverToStop.StopServerBroadcast();
            serverToStop.Stop();
            _servers.RemoveAt(0);
            
            // Wait for timeout (5 seconds + buffer)
            yield return new WaitForSeconds(6f);
            
            // Check that the server was removed
            List<ServerInfo> remainingServers = _client.GetDiscoveredServers();
            Debug.Log($"Initial servers: {initialServers.Count}, Remaining servers: {remainingServers.Count}");
            
            Assert.Less(remainingServers.Count, initialServers.Count, "Server was not removed after timeout");
            Assert.AreEqual(initialServers.Count - 1, remainingServers.Count, "Expected exactly one server to be removed");
            
            // Verify the stopped server is not in the remaining servers
            foreach (var server in remainingServers)
            {
                Assert.AreNotEqual(serverToStop.GetServerInfo().EndPoint, server.EndPoint, 
                    "Stopped server is still in the discovered servers list");
            }
        }

        [TearDown]
        public void TearDown()
        {
            _client.Stop();
            foreach (var server in _servers)
            {
                server.Stop();
            }
        }
    }
}
