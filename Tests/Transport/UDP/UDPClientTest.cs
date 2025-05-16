using System.Collections;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace TransportTest
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
            ITransport.OnClientConnected += OnClientConnected;
            ITransport.OnClientDisconnected += OnClientDisconnected;
            // Create and start the client
            _client = new UDPSolution();
            _client.Setup(Port, false);
            _client.Start();
        }


        [UnityTest]
        public IEnumerator TestClientConnected()
        {
            _client.Connect("localhost");

            yield return new WaitForSeconds(1f);
            
            Assert.IsTrue(_connected, "Client did not connect.");
        }
        
        [UnityTest]
        public IEnumerator TestClientDisconnected()
        {
            _client.Connect("localhost");
            yield return new WaitForSeconds(1f);
            
            _client.Disconnect();
            yield return new WaitForSeconds(1f);
            Assert.IsTrue(!_connected, "Client did not disconnect.");
        }
        
        [UnityTest]
        public IEnumerator TestMessageClientToServer()
        {
            _client.Connect("localhost");
            yield return new WaitForSeconds(1f);
        
            _client.Send(System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            yield return new WaitForSeconds(1f);

            string receivedMessage = System.Text.Encoding.ASCII.GetString(_server.Receive());
            Assert.AreEqual(TestMessage, receivedMessage, "Received message.");
        }
        
        
        [TearDown]
        public void TearDown()
        {
            _server?.Stop();
            _client?.Stop();
            ITransport.OnClientConnected -= OnClientConnected;
            ITransport.OnClientDisconnected -= OnClientDisconnected;
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
