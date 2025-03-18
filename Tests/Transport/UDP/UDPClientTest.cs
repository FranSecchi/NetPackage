using System.Collections;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace TransportTest.NetPackage.Tests.Transport.UDP
{
    public class UDPClientTest
    {
        private const int Port = 7777;
        private const string TestMessage = "Hello, Server!";
        
        private ITransport _server;
        private ITransport _client;
        private bool _connected = false;
        
        [SetUp]
        public void SetUp()
        {
            // Create and start the server
            _server = new UDPSolution();
            _server.Setup(Port, true);
            _server.Start();
            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
            // Create and start the client
            _client = new UDPSolution();
            _client.Setup(Port, false);
            _client.Start();
        }


        [UnityTest]
        public IEnumerator TestClientConnected()
        {
            // Client connects to the server
            _client.Connect("localhost", Port);

            // Wait a bit to allow connection
            yield return new WaitForSeconds(1f);
            
            //Assert
            Assert.IsTrue(_connected, "Client did not connect.");
        }
        
        [UnityTest]
        public IEnumerator TestClientDisconnected()
        {
            _client.Connect("localhost", Port);
            yield return new WaitForSeconds(1f);
            
            _client.Disconnect();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(!_connected, "Client did not disconnect.");
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
        
        [TearDown]
        public void TearDown()
        {
            _server?.Disconnect();
            _client?.Disconnect();
        }
        
        private void OnClientConnected(int id)
        {
            _connected = true;
        }
        private void OnClientDisconnected(int id)
        {
            _connected = false;
        }
    }
}
