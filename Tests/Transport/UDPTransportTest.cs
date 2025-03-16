using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LiteNetLib;
using NetPackage.Runtime.Transport;

namespace NetPackage.Tests.Transport
{
        public class UDPTransportTest
    {
        private ITransport server;
        private ITransport client;
        private bool clientConnected = false;
        private bool messageReceived = false;
        private string receivedMessage = "";

        private const int Port = 7777;
        private const string TestMessage = "Hello, Server!";

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
            server.Setup(Port, true);
            client.Start();
        }

        private void OnClientConnected()
        {
            clientConnected = true;
        }

        private void OnDataReceived(NetPacketReader reader)
        {
            receivedMessage = reader.GetString();
            messageReceived = true;
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
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(clientConnected, "Client did not connect.");
        }

        // [UnityTest]
        // public IEnumerator TestMessageExchange()
        // {
        //     // Ensure client is connected
        //     client.Connect("localhost", Port);
        //     yield return new WaitForSeconds(1f);
        //
        //     // Send a test message
        //     NetDataWriter writer = new NetDataWriter();
        //     writer.Put(TestMessage);
        //     client.Send(writer);
        //
        //     // Wait for message to be received
        //     yield return new WaitForSeconds(1f);
        //
        //     Assert.IsTrue(messageReceived, "Message was not received.");
        //     Assert.AreEqual(TestMessage, receivedMessage, "Received message does not match.");
        // }

        [TearDown]
        public void TearDown()
        {
            server?.Disconnect();
            client?.Disconnect();
        }
    }
}
