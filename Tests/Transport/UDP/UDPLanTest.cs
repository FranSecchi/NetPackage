using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;
namespace TransportTest
{
    public class UDPLanTest
    {
        private const int Port = 7777;
        private List<ITransport> _servers = new List<ITransport>();
        private ITransport _client;
        private bool _connected = false;
        
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            for (int i = 0; i < 3; i++)
            {
                ITransport server = new UDPSolution();
                server.Setup(Port + i, true, true);
                server.Start();
                server.SetServerInfo(new ServerInfo(){ServerName = "Name"});
                server.BroadcastServerInfo();
                _servers.Add(server);
                yield return new WaitForSeconds(1f);
            }
            
            _client = new UDPSolution();
            _client.Setup(Port, false, true);
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

        [UnityTest] public IEnumerator TestDiscoverMultipleServers()
        {
            yield return new WaitForSeconds(0.2f);
            List<ServerInfo> discoveredServers = _client.GetDiscoveredServers();

            Debug.Log($"Discovered servers: {string.Join(", ", discoveredServers)}");
            Assert.GreaterOrEqual(discoveredServers.Count, 2, "Expected multiple servers, but found less.");
            Assert.AreEqual(discoveredServers.Count, new HashSet<ServerInfo>(discoveredServers).Count, "Duplicate servers detected.");
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
