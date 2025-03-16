using System.Collections;
using System.Collections.Generic;
using NetPackage.Runtime.Transport;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace TransportTest.NetPackage.Tests.Transport.UDP
{
    public class UDPHost
    {
        private ITransport _server;
        private List<int> _connectedClients;
        
        private const int Port = 7777;
        private const string TestMessage = "Hello, Server!";
        
        [SetUp]
        public void SetUp()
        {
            _connectedClients = new List<int>();
            
            // Create and start the server
            _server = new UDPSolution();
            _server.Setup(Port, true);
            _server.Start();
            _server.OnClientConnected += OnClientConnected;
            _connectedClients = new List<int>();
            
        }
        
        [UnityTest]
        public IEnumerator TestServerUp()
        {
            Assert.IsNotNull(_server, "Server instance is null.");
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator TestMultipleClients()
        {
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(Port, false);
                client.Start();
                client.Connect("localhost", Port);
            }
            yield return new WaitForSeconds(2f);
            
            Assert.IsTrue(_connectedClients.Count == 5, "There should be 5 clients.");
        }
        
        [TearDown]
        public void TearDown()
        {
            _server?.Disconnect();
        }
        
        private void OnClientConnected(int id)
        {
            _connectedClients.Add(id);
        }
    }
}
