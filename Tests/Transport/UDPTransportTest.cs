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
        private ITransport server;
        private ITransport client;
        private bool clientConnected = false;

        private const int Port = 7777;

        [SetUp]
        public void SetUp()
        {
            // Create and start the server
            server = new UDPSolution();
            server.Setup(Port, true);
            server.Start();
            server.OnClientConnected += OnClientConnected;

            // Create and start the client
            client = new UDPSolution();
            client.Setup(Port, false);
            client.Start();
        }

        private void OnClientConnected()
        {
            clientConnected = true;
        }

        [UnityTest]
        public IEnumerator TestServerUp()
        {
            Assert.IsNotNull(server, "Server instance is null.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestClientConnected()
        {
            // Client connects to the server
            client.Connect("localhost", Port);

            // Wait a bit to allow connection
            float time = 0f;
            while (!clientConnected && time < 5f)
            {
                server.Listen();
                client.Listen();
                time += Time.deltaTime;
                yield return null;
            }
            
            //Assert
            Assert.IsTrue(clientConnected, "Client did not connect.");
        }
        
        [TearDown]
        public void TearDown()
        {
            server?.Disconnect();
            client?.Disconnect();
        }
    }
}
