using System.Collections;
using System.Collections.Generic;
using MessagePack;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SerializerTest.NetPackage.Tests.Serializer
{
    public class NetworkMessageTests
    {
        private NetManager _manager;
        private List<ITransport> _clients;
        [SetUp]
        public void Setup()
        {
            _manager = new GameObject().AddComponent<NetManager>();
            NetManager.SetTransport(new UDPSolution());
            _manager.address = "localhost";
            _manager.StartHost();
            _clients = new List<ITransport>();
            for (int i = 0; i < 5; i++)
            {
                ITransport client = new UDPSolution();
                client.Setup(NetManager.Port, false);
                client.Start();
                _clients.Add(client);
            }

        }


        [UnityTest]
        public IEnumerator SendNetworkMessageTest()
        {
            ConnectClients();
            NetMessage netMessage = new TestMessage(20, "Hello World");
            yield break;
        }
        private IEnumerator ConnectClients()
        {
            foreach (ITransport client in _clients)
            {
                client.Connect(_manager.address);
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
    
    public class TestMessage : NetMessage
    {
        [Key(0)] public int health;
        [Key(1)] public string msg;

        public TestMessage() { }

        public TestMessage(int health, string msg)
        {
            this.health = health;
            this.msg = msg;
        }
    }
}