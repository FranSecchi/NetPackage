using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Runtime;
using Runtime.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    /// <summary>
    /// Tests for state synchronization functionality in the networking system
    /// </summary>
    public class ServerSyncTests
    {
        private NetPrefabRegistry prefabs;
        private ITransport client;
        private GameObject testObj;
        private NetMessage received;
        private const int CLIENT_ID = 0;

        /// <summary>
        /// Sets up the test environment with NetManager, scene loading, and client connection
        /// </summary>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            
            SceneManager.LoadScene("TestScene");
            yield return new WaitForSeconds(0.2f);
            
            RegisterPrefab();
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return null;
        }

        /// <summary>
        /// Tests if host can send single state update to client
        /// </summary>
        [UnityTest]
        public IEnumerator SendSingleUpdate()
        {
            yield return WaitConnection();
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            
            TestObj testComponent = objs[0];
            testComponent.Set(42, 100, "test");
            StateManager.SendUpdateStates();
            
            yield return WaitValidate(typeof(SyncMessage));

            SyncMessage syncMsg = (SyncMessage)received;
            Assert.AreEqual(testComponent.NetObject.NetId, syncMsg.ObjectID, "Wrong object ID in sync message");
            Assert.Greater(syncMsg.changedValues.Count, 0, "No state changes in sync message");
        }

        /// <summary>
        /// Tests if host can send updates for multiple components
        /// </summary>
        [UnityTest]
        public IEnumerator SendMultipleUpdate()
        {
            yield return WaitConnection();
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            var comp1 = objs[0];
            var comp2 = objs[0].gameObject.AddComponent<TestObj>();
            
            yield return new WaitForSeconds(0.2f);
            
            comp1.Set(100, 1000, "first_changed");
            comp2.Set(200, 2000, "second_changed");
            
            StateManager.SendUpdateStates();

            yield return WaitValidate(typeof(SyncMessage));
            SyncMessage syncMsg = (SyncMessage)received;
            Assert.Greater(syncMsg.changedValues.Count, 0, "No state changes in sync message");

            yield return WaitValidate(typeof(SyncMessage));
            syncMsg = (SyncMessage)received;
            Assert.Greater(syncMsg.changedValues.Count, 0, "No state changes in sync message for second component");
        }

        /// <summary>
        /// Tests if host can receive and apply state updates from client
        /// </summary>
        [UnityTest]
        public IEnumerator ReceiveSingleUpdate()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            TestObj testComponent = objs[0];
            int initialHealth = testComponent.health;

            // Create and send sync message from client
            Dictionary<string, object> changes = new Dictionary<string, object> { { "health", 50 } };
            NetMessage syncMsg = new SyncMessage(testComponent.NetObject.NetId, 0, changes);
            client.Send(NetSerializer.Serialize(syncMsg));

            // Wait for message to be processed
            float startTime = Time.time;
            while (testComponent.health == initialHealth && Time.time - startTime < 1f)
            {
                yield return null;
            }

            Assert.AreEqual(50, testComponent.health, "State update not applied");
            Assert.AreNotEqual(initialHealth, testComponent.health, "Health value unchanged");
        }

        /// <summary>
        /// Tests if host can receive and apply updates for multiple components from client
        /// </summary>
        [UnityTest]
        public IEnumerator ReceiveMultipleUpdate()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            var comp1 = objs[0];
            var comp2 = objs[0].gameObject.AddComponent<TestObj>();
            // Create and send sync messages from client
            Dictionary<string, object> changes1 = new Dictionary<string, object> { { "health", 150 }, { "msg", "changed1" } };
            Dictionary<string, object> changes2 = new Dictionary<string, object> { { "health", 250 }, { "msg", "changed2" } };
            
            NetMessage syncMsg = new SyncMessage(comp1.NetObject.NetId, 0, changes1);
            NetMessage syncMsg1 = new SyncMessage(comp2.NetObject.NetId, 1, changes2);
            client.Send(NetSerializer.Serialize(syncMsg));
            client.Send(NetSerializer.Serialize(syncMsg1));

            // Wait for messages to be processed
            float startTime = Time.time;
            while ((comp1.health != 150 || comp2.health != 250) && Time.time - startTime < 1f)
            {
                yield return null;
            }

            Assert.AreEqual(150, comp1.health, "First component update not applied");
            Assert.AreEqual("changed1", comp1.msg, "First component message not updated");
            Assert.AreEqual(250, comp2.health, "Second component update not applied");
            Assert.AreEqual("changed2", comp2.msg, "Second component message not updated");
        }

        /// <summary>
        /// Tests if ownership changes are properly synchronized
        /// </summary>
        [UnityTest]
        public IEnumerator OwnershipChangeTest()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            var testComponent = objs[0];

            // Change ownership to client
            testComponent.NetObject.GiveOwner(CLIENT_ID);
            yield return WaitValidate(typeof(OwnershipMessage));


            OwnershipMessage ownerMsg = (OwnershipMessage)received;
            Assert.AreEqual(CLIENT_ID, ownerMsg.newOwnerId, "Wrong owner ID in message");
            Assert.AreEqual(testComponent.NetObject.NetId, ownerMsg.netObjectId, "Wrong object ID in ownership message");
        }

        /// <summary>
        /// Tests if state is properly recovered after client disconnection and reconnection
        /// </summary>
        [UnityTest]
        public IEnumerator StateRecoveryAfterDisconnect()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var testComponent = objs[0];
            testComponent.Set(999, 888, "test_before_disconnect");

            // Simulate disconnection
            client.Stop();
            yield return new WaitForSeconds(0.5f);

            // Reconnect client
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return WaitConnection();


            // Verify state values
            Assert.AreEqual(888, testComponent.health, "Health not recovered after reconnect");
            Assert.AreEqual(999, testComponent.id, "Id not recovered after reconnect");
            Assert.AreEqual("test_before_disconnect", testComponent.msg, "Message not recovered after reconnect");
        }

        /// <summary>
        /// Cleans up test environment after each test
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            NetManager.StopNet();
            client.Stop();
            Messager.ClearHandlers();
            
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj);
            }
        }

        /// <summary>
        /// Waits for client connection to be established
        /// </summary>
        private IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return WaitValidate(typeof(SpawnMessage));
            client.Send(NetSerializer.Serialize(received));
            yield return new WaitForSeconds(0.2f);
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
        /// <summary>
        /// Registers test prefab with NetScene
        /// </summary>
        private void RegisterPrefab()
        {
            var prefab = Resources.Load<GameObject>("TestObj");
            prefabs = ScriptableObject.CreateInstance<NetPrefabRegistry>();
            prefabs.prefabs.Add(prefab);
            NetScene.Instance.RegisterPrefabs(prefabs.prefabs);
        }
    }
} 