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
        private List<ITransport> _servers;
        private ITransport _client;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _servers = new List<ITransport>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_client != null)
            {
                _client.StopServerDiscovery();
                _client.Stop();
            }
            
            if (_servers != null)
            {
                foreach (var server in _servers)
                {
                    server.StopServerBroadcast();
                    server.Stop();
                }
                _servers.Clear();
            }
            yield return null;
        }

        private IEnumerator SetupServersAndClient()
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
            _client.StartServerDiscovery(0.1f);
            yield return new WaitForSeconds(2f);
        }

        [UnityTest]
        public IEnumerator TestDiscoverServer()
        {
            yield return SetupServersAndClient();
            yield return new WaitForSeconds(0.2f);
            Assert.IsNotEmpty(_client.GetDiscoveredServers(), "GetDiscoveredServers failed");
        }

        [UnityTest] 
        public IEnumerator TestDiscoverMultipleServers()
        {
            yield return SetupServersAndClient();
            yield return new WaitForSeconds(0.2f);
            List<ServerInfo> discoveredServers = _client.GetDiscoveredServers();

            Debug.Log($"Discovered servers: {string.Join(", ", discoveredServers)}");
            Assert.GreaterOrEqual(discoveredServers.Count, 2, "Expected multiple servers, but found less.");
            Assert.AreEqual(discoveredServers.Count, new HashSet<ServerInfo>(discoveredServers).Count, "Duplicate servers detected.");
        }

        [UnityTest]
        public IEnumerator TestServerTimeout()
        {
            yield return SetupServersAndClient();
            
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
                Assert.AreNotEqual(serverToStop.GetServerInfo(), server, 
                    "Stopped server is still in the discovered servers list");
            }
        }

        [UnityTest]
        public IEnumerator TestServerInfoUpdate()
        {
            yield return SetupServersAndClient();
            
            // Wait for initial server discovery
            yield return new WaitForSeconds(2f);
            List<ServerInfo> initialServers = _client.GetDiscoveredServers();
            Assert.IsNotEmpty(initialServers, "No servers discovered initially");
            Debug.Log($"Discovered servers: {string.Join(", ", initialServers)}");
            
            // Get the first server and its initial info
            var server = _servers[0];
            var initialServerInfo = server.GetServerInfo();
            Debug.Log($"Initial server info: {initialServerInfo}");
            
            // Update server info
            var newServerInfo = new ServerInfo
            {
                Address = initialServerInfo.Address,
                Port = initialServerInfo.Port,
                ServerName = "Updated Server Name",
                CurrentPlayers = 5,
                MaxPlayers = 10,
                Ping = initialServerInfo.Ping,
                GameMode = "New Game Mode",
                CustomData = new Dictionary<string, string> { { "key", "value" } }
            };
            server.SetServerInfo(newServerInfo);
            Debug.Log($"New server info: {server.GetServerInfo()}");
            // Wait for the update to propagate
            yield return new WaitForSeconds(5f);
            
            // Get updated server list
            List<ServerInfo> updatedServers = _client.GetDiscoveredServers();
            
            // Find the updated server in the client's list
            var updatedServerInfo = updatedServers.Find(s => s.Equals(initialServerInfo));
            Assert.IsNotNull(updatedServerInfo, "Server not found in updated list");
            Debug.Log($"Updated server info: {updatedServerInfo}");
            
            // Verify the info was updated
            Assert.AreEqual(newServerInfo.ServerName, updatedServerInfo.ServerName, "Server name was not updated");
            Assert.AreEqual(newServerInfo.CurrentPlayers, updatedServerInfo.CurrentPlayers, "Current players was not updated");
            Assert.AreEqual(newServerInfo.MaxPlayers, updatedServerInfo.MaxPlayers, "Max players was not updated");
            Assert.AreEqual(newServerInfo.GameMode, updatedServerInfo.GameMode, "Game mode was not updated");
            Assert.IsTrue(updatedServerInfo.CustomData.ContainsKey("key"), "Custom data was not updated");
            Assert.AreEqual("value", updatedServerInfo.CustomData["key"], "Custom data value was not updated");
        }
    }
}
