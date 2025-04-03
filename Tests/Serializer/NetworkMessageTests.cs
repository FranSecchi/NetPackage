using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MessagePack;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SerializerTest
{
    public class NetworkMessageTests
    {
        private NetManager _manager;
        private List<ITransport> _clients;
        private TestMsg received;
        [SetUp]
        public void Setup()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            received = null;
        }



        [UnityTest]
        public IEnumerator Server_SendMsg()
        {
            yield return new WaitForSeconds(0.5f);
            StartHost();
            yield return new WaitForSeconds(0.5f);
            yield return ConnectClients();
            
            TestMsg testMsg = new TestMsg(null, 34, "Hello World");
            
            Messager.RegisterHandler<TestMsg>(OnReceived);
            NetManager.Send(testMsg);
            yield return new WaitForSeconds(0.5f);
            
            foreach (ITransport client in _clients)
            {
                byte[] data = client.Receive();
                Assert.IsNotNull(data);
                Messager.HandleMessage(data);
            }
            
            Assert.IsNotNull(received, "Received message is null");
            Assert.IsTrue(testMsg.ObjectID == received.ObjectID, "Object ID is incorrect");
            Assert.IsTrue(testMsg.msg.Equals(received.msg), "Object ID is incorrect");
        }
        [UnityTest]
        public IEnumerator Server_ReceiveMsg()
        {
            yield return new WaitForSeconds(0.5f);
            StartHost();
            yield return new WaitForSeconds(0.5f);
            
            Messager.RegisterHandler<TestMsg>(OnReceived);
            yield return ConnectClients();
            yield return new WaitForSeconds(0.5f);
            
            TestMsg testMsg = new TestMsg(null, 34, "Hello World");
            byte[] data = NetSerializer.Serialize((NetMessage) testMsg);
            foreach (ITransport client in _clients)
            {
                client.Send(data);
            }
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsNotNull(received, "Received message is null");
            Assert.IsTrue(testMsg.ObjectID == received.ObjectID, "Object ID is incorrect");
            Assert.IsTrue(testMsg.msg.Equals(received.msg), "Object ID is incorrect");
        }
        [UnityTest]
        public IEnumerator Client_SendMsg()
        {
            yield return new WaitForSeconds(0.5f);
            StartClient();
            yield return new WaitForSeconds(0.5f);
            
            TestMsg testMsg = new TestMsg(null, 34, "Hello World");
            
            Messager.RegisterHandler<TestMsg>(OnReceived);
            NetManager.Send(testMsg);
            yield return new WaitForSeconds(0.5f);
            
            foreach (ITransport client in _clients)
            {
                byte[] data = client.Receive();
                Assert.IsNotNull(data);
                Messager.HandleMessage(data);
            }
            
            Assert.IsNotNull(received, "Received message is null");
            Assert.IsTrue(testMsg.ObjectID == received.ObjectID, "Object ID is incorrect");
            Assert.IsTrue(testMsg.msg.Equals(received.msg), "Object ID is incorrect");
        }
        [UnityTest]
        public IEnumerator Client_ReceiveMsg()
        {
            yield return new WaitForSeconds(0.5f);
            StartClient();
            yield return new WaitForSeconds(0.5f);
            
            Messager.RegisterHandler<TestMsg>(OnReceived);
            
            TestMsg testMsg = new TestMsg(null, 34, "Hello World");
            byte[] data = NetSerializer.Serialize((NetMessage) testMsg);
            foreach (ITransport client in _clients)
            {
                client.Send(data);
            }
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsNotNull(received, "Received message is null");
            Assert.IsTrue(testMsg.ObjectID == received.ObjectID, "Object ID is incorrect");
            Assert.IsTrue(testMsg.msg.Equals(received.msg), "Object ID is incorrect");
        }

        [TearDown]
        public void TearDown()
        {
            NetManager.StopHosting();
            NetManager.StopClient();
    
            if (_clients != null)
            {
                foreach (ITransport client in _clients)
                {
                    client.Disconnect(); 
                }
                _clients.Clear(); 
            }

    
            _clients = new List<ITransport>();
            received = null;

        }
        private void OnReceived(TestMsg obj)
        {
            received = obj;
        }

        private void StartClient()
        {
            _clients = new List<ITransport>();
            ITransport server = new UDPSolution();
            server.Setup(NetManager.Port, true);
            server.Start();
            _clients.Add(server);
            NetManager.StartClient();
        }

        private void StartHost()
        {
            NetManager.StartHost();
            _clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(NetManager.Port, false);
                client.Start();
                _clients.Add(client);
            }
        }
        private IEnumerator ConnectClients()
        {
            yield return new WaitForSeconds(0.5f);
            foreach (ITransport client in _clients)
            {
                client.Connect(_manager.address);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public class TestMsg : NetMessage
    {
        [Key(1)] public int ObjectID;
        [Key(2)] public string msg;

        public TestMsg(){}
        public TestMsg(List<int> s, int i, string msg) : base(s)
        {
            ObjectID = i;
            this.msg = msg;
        }
    }
}