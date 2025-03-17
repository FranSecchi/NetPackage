using System.Collections;
using System.Collections.Generic;
using NetPackage.Runtime.Transport;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace TransportTest
{
    public class UDPHostTest
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
            _server = new global::Transport.NetPackage.Runtime.Transport.UDP.UDPHost();
            _server.Setup(Port);
            _server.Start();
            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;
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
                ITransport client = new UDPClient();
                client.Setup(Port);
                client.Start();
                client.Connect("localhost", Port);
            }
            yield return new WaitForSeconds(1f);
            
            Assert.IsTrue(_connectedClients.Count == 5, "There should be 5 clients.");
        }
        
        [UnityTest]
        public IEnumerator TestMessageServerToClient()
        {
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPClient();
                client.Setup(Port);
                client.Start();
                client.Connect("localhost", Port);
                clients.Add(client);
                yield return new WaitForSeconds(0.5f);
            }
        
            _server.SendTo(_connectedClients[2], System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            yield return new WaitForSeconds(1f);
        
            string receivedMessage = System.Text.Encoding.ASCII.GetString(clients[2].Receive());
            Assert.AreEqual(TestMessage, receivedMessage, "Message did not match.");
            
            for (int i = 0; i < 5; i++)
            {
                clients[i].Disconnect();
            }
        }
        
        [UnityTest]
        public IEnumerator TestMessageServerToAllClient()
        {
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPClient();
                client.Setup(Port);
                client.Start();
                client.Connect("localhost", Port);
                clients.Add(client);
                yield return new WaitForSeconds(0.5f);
            }
            
            // Send a test message
            _server.Send(System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            // Wait for message to be received
            yield return new WaitForSeconds(1f);

            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                count += System.Text.Encoding.ASCII.GetString(clients[i].Receive()) != "" ? 1 : 0;
            }
            
            Assert.AreEqual(count, 5, "Messages dropped: "+(5-count)+".");
            
            for (int i = 0; i < 5; i++)
            {
                clients[i].Disconnect();
            }
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
        private void OnClientDisconnected(int id)
        {
            _connectedClients.Remove(id);
        }
    }
}
