using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NetPackage.Runtime.Transport;

namespace NetPackage.Tests.Transport
{
        public class UDPTransportTest
    {
        private ITransport _server;
        private ITransport _client;
        private bool _clientConnected = false;

        private const int Port = 7777;
        private const string TestMessage = "Hello, Server!";

        [SetUp]
        public void SetUp()
        {
            // Create and start the server
            _server = new UDPSolution();
            _server.Setup(Port, true);
            _server.Start();
            _server.OnClientConnected += OnClientConnected;
            // Create and start the client
            _client = new UDPSolution();
            _client.Setup(Port, false);
            _client.Start();
        }

        private void OnClientConnected()
        {
            _clientConnected = true;
        }

        [UnityTest]
        public IEnumerator TestServerUp()
        {
            Assert.IsNotNull(_server, "Server instance is null.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientConnected()
        {
            // Client connects to the server
            _client.Connect("localhost", Port);

            // Wait a bit to allow connection
            yield return new WaitForSeconds(1f);
            
            //Assert
            Assert.IsTrue(_clientConnected, "Client did not connect.");
            
            _client.Disconnect();
        }

        [UnityTest]
        public IEnumerator TestMessageClientToServer()
        {
            // Ensure client is connected
            _client.Connect("localhost", Port);
            yield return new WaitForSeconds(1f);
        
            // Send a test message
            _client.Send(System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            // Wait for message to be received
            yield return new WaitForSeconds(1f);

            string receivedMessage = System.Text.Encoding.ASCII.GetString(_server.Receive());
            Assert.AreEqual(TestMessage, receivedMessage, "Received message.");
            
            _client.Disconnect();
        }
        
        [UnityTest]
        public IEnumerator TestMessageServerToAllClient()
        {
            // Ensure client is connected
            _client.Connect("localhost", Port);
            yield return new WaitForSeconds(1f);
        
            // Send a test message
            _server.Send(System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            // Wait for message to be received
            yield return new WaitForSeconds(1f);

            string receivedMessage = System.Text.Encoding.ASCII.GetString(_client.Receive());
            Assert.AreEqual(TestMessage, receivedMessage, "Received message.");
            
            _client.Disconnect();
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Disconnect();
            _client?.Disconnect();
        }
    }
}
