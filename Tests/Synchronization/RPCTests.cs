using System;
using System.Collections;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class RPCTests
    {
        private ITransport client;
        private TestRPCBehaviour testObj;
        private TestRPCBehaviour clientTestObj;
        private NetMessage received;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            
            var obj = new GameObject();
            testObj = obj.AddComponent<TestRPCBehaviour>();
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return WaitConnection();
            yield return WaitValidate(typeof(SpawnMessage));
            Assert.IsTrue(received is SpawnMessage, "Client did not receive spawn message");
            client.Send(NetSerializer.Serialize(received));
            yield return new WaitForSeconds(0.2f);
        }

        [UnityTest]
        public IEnumerator TestClientToServerRPC()
        {
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "TestRPC", 42, "test");
            client.Send(NetSerializer.Serialize(msg));
            
            float startTime = Time.time;
            while (testObj.lastReceivedValue == 0 && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.AreEqual(42, testObj.lastReceivedValue, "Server did not receive correct value from client RPC");
            Assert.AreEqual("test", testObj.lastReceivedMessage, "Server did not receive correct message from client RPC");
        }

        [UnityTest]
        public IEnumerator TestServerToClientRPC()
        {
            testObj.lastReceivedValue = 0;
            testObj.lastReceivedMessage = "";
            
            testObj.CallTestRPC(100, "server_test");
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            float startTime = Time.time;
            while (testObj.lastReceivedValue == 0 && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            
            RPCMessage msg = (RPCMessage)received;
            Assert.AreEqual(100, msg.Parameters[0], "Client did not receive correct value from server RPC");
            Assert.AreEqual("server_test", msg.Parameters[1], "Client did not receive correct message from server RPC");
            
            Assert.AreEqual(100, testObj.lastReceivedValue, "Server did not call RPC itself");
            Assert.AreEqual("server_test", testObj.lastReceivedMessage, "Server did not call RPC itself");
        }

        [UnityTest]
        public IEnumerator TestServerOnlyRPC()
        {
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "ServerOnlyRPC");
            client.Send(NetSerializer.Serialize(msg));
            
            float startTime = Time.time;
            while (!testObj.serverOnlyCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsTrue(testObj.serverOnlyCalled, "Server-only RPC was not called on server");
        }

        [UnityTest]
        public IEnumerator TestClientOnlyRPC()
        {
            testObj.clientOnlyCalled = false;
            
            testObj.CallClientOnlyRPC();
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            float startTime = Time.time;
            while (!testObj.clientOnlyCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsFalse(testObj.clientOnlyCalled, "Client-only RPC was not called on client");
        }

        private IEnumerator WaitConnection()
        {
            client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
        }

        [TearDown]
        public void TearDown()
        {
            if (testObj != null && testObj.NetObject != null)
            {
                RPCManager.Unregister(testObj.NetObject.NetId, testObj);
            }

            NetManager.StopHosting();
            client.Stop();
            Messager.ClearHandlers();
            
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj);
            }

            testObj = null;
            clientTestObj = null;
            received = null;
        }
        
        private IEnumerator WaitValidate(Type expectedType)
        {
            byte[] data = null;
            float startTime = Time.time;
            NetMessage msg = null;
            while (Time.time - startTime < 1f)
            {
                data = client.Receive();
                if (data != null)
                {
                    msg = NetSerializer.Deserialize<NetMessage>(data);
                    if (msg.GetType() == expectedType)
                    {
                        received = msg;
                        yield break;
                    }
                }
                yield return null;
            }
            
            Assert.IsTrue(msg != null && msg.GetType() == expectedType, 
                $"Expected message of type {expectedType.Name} but got {(msg == null ? "null" : msg.GetType().Name)}");
        }
    }

    public class TestRPCBehaviour : NetBehaviour
    {
        public int lastReceivedValue;
        public string lastReceivedMessage;
        public bool serverOnlyCalled;
        public bool clientOnlyCalled;

        [NetRPC]
        public void TestRPC(int value, string message)
        {
            lastReceivedValue = value;
            lastReceivedMessage = message;
        }

        [NetRPC(serverOnly: true)]
        public void ServerOnlyRPC()
        {
            serverOnlyCalled = true;
        }

        [NetRPC(clientOnly: true)]
        public void ClientOnlyRPC()
        {
            clientOnlyCalled = true;
        }

        public void CallTestRPC(int value, string message)
        {
            CallRPC("TestRPC", value, message);
        }

        public void CallServerOnlyRPC()
        {
            CallRPC("ServerOnlyRPC");
        }

        public void CallClientOnlyRPC()
        {
            CallRPC("ClientOnlyRPC");
        }
    }
} 