using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;
using MessagePack;

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
        public IEnumerator TestBidirectionalRPC()
        {
            // Test client to server
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "TestRPC", null, 42, "test");
            client.Send(NetSerializer.Serialize(msg));
            
            float startTime = Time.time;
            while (testObj.lastReceivedValue == 0 && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.AreEqual(42, testObj.lastReceivedValue, "Server did not receive correct value from client RPC");
            Assert.AreEqual("test", testObj.lastReceivedMessage, "Server did not receive correct message from client RPC");

            // Test server to client
            testObj.lastReceivedValue = 0;
            testObj.lastReceivedMessage = "";
            
            testObj.CallTestRPC(100, "server_test");
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            
            RPCMessage rpcMsg = (RPCMessage)received;
            Assert.AreEqual(100, rpcMsg.Parameters[0], "Client did not receive correct value from server RPC");
            Assert.AreEqual("server_test", rpcMsg.Parameters[1], "Client did not receive correct message from server RPC");
        }

        [UnityTest]
        public IEnumerator TestServerToClientRPC()
        {
            testObj.serverToClientCalled = false;
            
            testObj.CallServerToClientRPC();
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
        }

        [UnityTest]
        public IEnumerator TestClientToServerRPC()
        {
            testObj.clientToServerCalled = false;
            
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "ClientToServerRPC");
            client.Send(NetSerializer.Serialize(msg));
            
            float startTime = Time.time;
            while (!testObj.clientToServerCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsTrue(testObj.clientToServerCalled, "Client-to-server RPC was not called on server");
        }

        [UnityTest]
        public IEnumerator TestTargetModeAll()
        {
            testObj.targetModeAllCalled = false;
            testObj.targetModeAllCallCount = 0;
            
            testObj.CallTargetModeAllRPC();
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            float startTime = Time.time;
            while (!testObj.targetModeAllCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsTrue(testObj.targetModeAllCalled, "TargetMode.All RPC was not called on server");
            Assert.AreEqual(1, testObj.targetModeAllCallCount, "TargetMode.All RPC was not called exactly once");
        }

        [UnityTest]
        public IEnumerator TestTargetModeSpecific()
        {
            testObj.targetModeSpecificCalled = false;
            testObj.targetModeSpecificCallCount = 0;
            
            // Test server sending to specific client
            var targetList = new List<int> { 0 }; // Send to client with ID 0
            testObj.CallTargetModeSpecificRPC(targetList);
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            
            // Test client sending to server
            testObj.targetModeSpecificCalled = false;
            testObj.targetModeSpecificCallCount = 0;
            
            var serverTargetList = new List<int> { NetManager.ConnectionId() };
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "TargetModeSpecificRPC", null, serverTargetList);
            client.Send(NetSerializer.Serialize(msg));
            
            float startTime = Time.time;
            while (!testObj.targetModeSpecificCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsTrue(testObj.targetModeSpecificCalled, "TargetMode.Specific RPC was not called on server");
            Assert.AreEqual(1, testObj.targetModeSpecificCallCount, "TargetMode.Specific RPC was not called exactly once");
        }

        [UnityTest]
        public IEnumerator TestTargetModeOthers()
        {
            testObj.targetModeOthersCalled = false;
            testObj.targetModeOthersCallCount = 0;
            
            // Test server sending to all clients except itself
            testObj.CallTargetModeOthersRPC();
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            float startTime = Time.time;
            while (!testObj.targetModeOthersCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsFalse(testObj.targetModeOthersCalled, "TargetMode.Others RPC was called on server when it shouldn't be");
            Assert.AreEqual(0, testObj.targetModeOthersCallCount, "TargetMode.Others RPC was called on server when it shouldn't be");
            
            // Test client sending to server
            testObj.targetModeOthersCalled = false;
            testObj.targetModeOthersCallCount = 0;
            
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "TargetModeOthersRPC");
            client.Send(NetSerializer.Serialize(msg));
            
            startTime = Time.time;
            while (!testObj.targetModeOthersCalled && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsTrue(testObj.targetModeOthersCalled, "TargetMode.Others RPC was not called on server");
            Assert.AreEqual(1, testObj.targetModeOthersCallCount, "TargetMode.Others RPC was not called exactly once");
        }

        [UnityTest]
        public IEnumerator TestComplexDataRPC()
        {
            // Create complex data
            var complexData = new TestRPCBehaviour.ComplexData
            {
                Id = 42,
                Name = "TestObject",
                Tags = new List<string> { "tag1", "tag2", "tag3" },
                Stats = new Dictionary<string, int>
                {
                    { "health", 100 },
                    { "mana", 50 },
                    { "stamina", 75 }
                }
            };

            // Test server to client
            testObj.CallComplexDataRPC(complexData);
            
            yield return WaitValidate(typeof(RPCMessage));
            Assert.IsTrue(received is RPCMessage, "Client did not receive RPC message");
            
            float startTime = Time.time;
            while (testObj.lastReceivedComplexData == null && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsNotNull(testObj.lastReceivedComplexData, "Complex data was not received");
            Assert.AreEqual(complexData.Id, testObj.lastReceivedComplexData.Id, "Complex data Id mismatch");
            Assert.AreEqual(complexData.Name, testObj.lastReceivedComplexData.Name, "Complex data Name mismatch");
            Assert.AreEqual(complexData.Tags.Count, testObj.lastReceivedComplexData.Tags.Count, "Complex data Tags count mismatch");
            Assert.AreEqual(complexData.Stats.Count, testObj.lastReceivedComplexData.Stats.Count, "Complex data Stats count mismatch");
            
            // Test client to server
            testObj.lastReceivedComplexData = null;
            
            var clientComplexData = new TestRPCBehaviour.ComplexData
            {
                Id = 123,
                Name = "ClientObject",
                Tags = new List<string> { "client", "test" },
                Stats = new Dictionary<string, int>
                {
                    { "score", 1000 },
                    { "level", 5 }
                }
            };
            
            NetMessage msg = new RPCMessage(0, testObj.NetObject.NetId, "ComplexDataRPC", null, clientComplexData);
            client.Send(NetSerializer.Serialize(msg));
            
            startTime = Time.time;
            while (testObj.lastReceivedComplexData == null && Time.time - startTime < 1f)
            {
                yield return null;
            }
            
            Assert.IsNotNull(testObj.lastReceivedComplexData, "Complex data was not received from client");
            Assert.AreEqual(clientComplexData.Id, testObj.lastReceivedComplexData.Id, "Client complex data Id mismatch");
            Assert.AreEqual(clientComplexData.Name, testObj.lastReceivedComplexData.Name, "Client complex data Name mismatch");
            Assert.AreEqual(clientComplexData.Tags.Count, testObj.lastReceivedComplexData.Tags.Count, "Client complex data Tags count mismatch");
            Assert.AreEqual(clientComplexData.Stats.Count, testObj.lastReceivedComplexData.Stats.Count, "Client complex data Stats count mismatch");
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

            NetManager.StopNet();
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
        public bool serverToClientCalled;
        public bool clientToServerCalled;
        public bool targetModeAllCalled;
        public int targetModeAllCallCount;
        public bool targetModeSpecificCalled;
        public int targetModeSpecificCallCount;
        public bool targetModeOthersCalled;
        public int targetModeOthersCallCount;
        public ComplexData lastReceivedComplexData;

        [MessagePackObject]
        public class ComplexData
        {
            [Key(0)] public int Id { get; set; }
            [Key(1)] public string Name { get; set; }
            [Key(2)] public List<string> Tags { get; set; }
            [Key(3)] public Dictionary<string, int> Stats { get; set; }
        }

        [NetRPC]
        public void TestRPC(int value, string message)
        {
            lastReceivedValue = value;
            lastReceivedMessage = message;
        }

        [NetRPC]
        public void ComplexDataRPC(ComplexData data)
        {
            lastReceivedComplexData = data;
        }

        [NetRPC(Direction.ServerToClient)]
        public void ServerToClientRPC()
        {
            serverToClientCalled = true;
        }

        [NetRPC(Direction.ClientToServer)]
        public void ClientToServerRPC()
        {
            clientToServerCalled = true;
        }

        [NetRPC(Direction.Bidirectional, Send.All)]
        public void TargetModeAllRPC()
        {
            targetModeAllCalled = true;
            targetModeAllCallCount++;
        }

        [NetRPC(Direction.Bidirectional, Send.Specific)]
        public void TargetModeSpecificRPC(List<int> targetId)
        {
            targetModeSpecificCalled = true;
            targetModeSpecificCallCount++;
        }

        [NetRPC(Direction.Bidirectional, Send.Others)]
        public void TargetModeOthersRPC()
        {
            targetModeOthersCalled = true;
            targetModeOthersCallCount++;
        }

        public void CallTestRPC(int value, string message)
        {
            CallRPC("TestRPC", value, message);
        }

        public void CallServerToClientRPC()
        {
            CallRPC("ServerToClientRPC");
        }

        public void CallClientToServerRPC()
        {
            CallRPC("ClientToServerRPC");
        }

        public void CallTargetModeAllRPC()
        {
            CallRPC("TargetModeAllRPC");
        }

        public void CallTargetModeSpecificRPC(List<int> targetId)
        {
            CallRPC("TargetModeSpecificRPC", targetId);
        }

        public void CallTargetModeOthersRPC()
        {
            CallRPC("TargetModeOthersRPC");
        }

        public void CallComplexDataRPC(ComplexData data)
        {
            CallRPC("ComplexDataRPC", data);
        }
    }
} 