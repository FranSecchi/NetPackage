using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace TransportTest
{
    public class UDPHostTest
    {
        private ITransport _transport;
        private List<int> _connectedClients;
        
        private const int Port = 7777;
        private const string TestMessage = "Hello, Server!";
        
        [SetUp]
        public void SetUp()
        {
            _connectedClients = new List<int>();
            
            _transport = new UDPSolution();
            _transport.Setup(Port, true);
            _transport.Start();
            ITransport.OnClientConnected += OnClientConnected;
            ITransport.OnClientDisconnected += OnClientDisconnected;
            _connectedClients = new List<int>();
            
        }
        
        [UnityTest]
        public IEnumerator TestServerUp()
        {
            Assert.IsNotNull(_transport, "Server instance is null.");
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator TestMultipleClients()
        {
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(Port, false);
                client.Start();
                client.Connect("localhost");
                clients.Add(client);
            }
            yield return new WaitForSeconds(0.2f);
            
            Assert.IsTrue(_connectedClients.Count == 5, "There should be 5 clients.");
            
            foreach (var client in clients)
            {
                client.Stop();
            }
        }
        
        [UnityTest]
        public IEnumerator TestMessageServerToClient()
        {
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(Port, false);
                client.Start();
                client.Connect("localhost");
                clients.Add(client);
            }
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < 5; i++)
            {
                _transport.SendTo(_connectedClients[i], System.Text.Encoding.ASCII.GetBytes(TestMessage));
                yield return new WaitForSeconds(0.2f);
                string receivedMessage = System.Text.Encoding.ASCII.GetString(clients[i].Receive());
                Assert.AreEqual(TestMessage, receivedMessage, "Message did not match.");
            }
            
            foreach (var client in clients)
            {
                client.Stop();
            }
        }
        
        [UnityTest]
        public IEnumerator TestMessageServerToAllClient()
        {
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(Port, false);
                client.Start();
                client.Connect("localhost");
                clients.Add(client);
            }
            yield return new WaitForSeconds(0.2f);
            
            _transport.Send(System.Text.Encoding.ASCII.GetBytes(TestMessage));
        
            yield return new WaitForSeconds(0.2f);

            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                count += System.Text.Encoding.ASCII.GetString(clients[i].Receive()) != "" ? 1 : 0;
            }
            
            Assert.AreEqual(count, 5, "Messages dropped: "+(5-count)+".");
            
            foreach (var client in clients)
            {
                client.Stop();
            }
        }

        [UnityTest]
        public IEnumerator TestKickClient()
        {  
            List<ITransport> clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(Port, false);
                client.Start();
                client.Connect("localhost");
                clients.Add(client);
            }
            yield return new WaitForSeconds(0.2f);
            _transport.Kick(2);
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(_connectedClients.Count == 4, "There should be 4 clients.");
            foreach (var client in clients)
            {
                client.Stop();
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            _transport?.Stop();
            ITransport.OnClientConnected -= OnClientConnected;
            ITransport.OnClientDisconnected -= OnClientDisconnected;
        }
        
        private void OnClientConnected(int id)
        {
            if (_connectedClients.Contains(id))
            {
                return;
            }
            _connectedClients.Add(id);
        }
        private void OnClientDisconnected(int id)
        {
            if (!_connectedClients.Contains(id))
            {
                return;
            }
            _connectedClients.Remove(id);
            Debug.Log(id + " disconnected." + _connectedClients.Count);
        }
    }
}
