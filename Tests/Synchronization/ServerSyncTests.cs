using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NetPackage.Network;
using NetPackage.Serializer;
using NetPackage.Messages;
using NetPackage.Synchronization;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class ServerSyncTests
    {
        private NetPrefabRegistry prefabs;
        private ITransport client;
        private GameObject testObj;
        private NetMessage received;
        private const int CLIENT_ID = 0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            
            new GameObject().AddComponent<TestObj>().Set(10,500,"init");
            yield return new WaitForSeconds(0.2f);
            
            RegisterPrefab();
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return null;
        }

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

        [UnityTest]
        public IEnumerator ReceiveSingleUpdate()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            TestObj testComponent = objs[0];
            int initialHealth = testComponent.health;

            Dictionary<string, object> changes = new Dictionary<string, object> { { "health", 50 } };
            NetMessage syncMsg = new SyncMessage(testComponent.NetObject.NetId, 0, changes);
            client.Send(NetSerializer.Serialize(syncMsg));

            float startTime = Time.time;
            while (testComponent.health == initialHealth && Time.time - startTime < 1f)
            {
                yield return null;
            }

            Assert.AreEqual(50, testComponent.health, "State update not applied");
            Assert.AreNotEqual(initialHealth, testComponent.health, "Health value unchanged");
        }

        [UnityTest]
        public IEnumerator ReceiveMultipleUpdate()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            var comp1 = objs[0];
            var comp2 = objs[0].gameObject.AddComponent<TestObj>();
            
            Dictionary<string, object> changes1 = new Dictionary<string, object> { { "health", 150 }, { "msg", "changed1" } };
            Dictionary<string, object> changes2 = new Dictionary<string, object> { { "health", 250 }, { "msg", "changed2" } };
            
            NetMessage syncMsg = new SyncMessage(comp1.NetObject.NetId, 0, changes1);
            NetMessage syncMsg1 = new SyncMessage(comp2.NetObject.NetId, 1, changes2);
            client.Send(NetSerializer.Serialize(syncMsg));
            client.Send(NetSerializer.Serialize(syncMsg1));

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

        [UnityTest]
        public IEnumerator OwnershipChangeTest()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Scene object not found");
            var testComponent = objs[0];

            testComponent.NetObject.GiveOwner(CLIENT_ID);
            yield return WaitValidate(typeof(OwnershipMessage));


            OwnershipMessage ownerMsg = (OwnershipMessage)received;
            Assert.AreEqual(CLIENT_ID, ownerMsg.newOwnerId, "Wrong owner ID in message");
            Assert.AreEqual(testComponent.NetObject.NetId, ownerMsg.netObjectId, "Wrong object ID in ownership message");
        }

        [UnityTest]
        public IEnumerator StateRecoveryAfterDisconnect()
        {
            yield return WaitConnection();

            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var testComponent = objs[0];
            testComponent.Set(999, 888, "test_before_disconnect");

            client.Stop();
            yield return new WaitForSeconds(0.5f);

            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return WaitConnection();


            Assert.AreEqual(888, testComponent.health, "Health not recovered after reconnect");
            Assert.AreEqual(999, testComponent.id, "Id not recovered after reconnect");
            Assert.AreEqual("test_before_disconnect", testComponent.msg, "Message not recovered after reconnect");
        }
        
        [TearDown]
        public void TearDown()
        {
            NetManager.StopNet();
            client.Stop();
            Messager.ClearHandlers();
            StateManager.Clear();
            
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj);
            }
        }

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
        
        private void RegisterPrefab()
        {
            var prefab = Resources.Load<GameObject>("TestObj");
            prefabs = ScriptableObject.CreateInstance<NetPrefabRegistry>();
            prefabs.prefabs.Add(prefab);
            NetScene.Instance.RegisterPrefabs(prefabs.prefabs);
        }
    }
} 