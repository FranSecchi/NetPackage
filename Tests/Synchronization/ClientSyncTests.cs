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
    public class ClientSyncTests
    {
        private NetPrefabRegistry prefabs;
        private ITransport host;
        private TestObj testObj;
        private NetMessage received;
        private const int CLIENT_ID = 0;
        private List<int> clientIds = new List<int>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            host = new UDPSolution();
            host.Setup(NetManager.Port, true,  new ServerInfo(){Address = "127.0.0.1", Port = NetManager.Port, MaxPlayers = 10});
            host.Start();
            yield return new WaitForSeconds(0.2f);
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartClient();

            RegisterPrefab();
            
            GameObject go = new GameObject().AddComponent<SceneObjectId>().gameObject;
            testObj = go.AddComponent<TestObj>();
            testObj.Set(10, 500, "init");
            yield return WaitConnection();
            
        }

        [UnityTest]
        public IEnumerator ReceiveStateUpdate()
        {
            yield return WaitConnection();
            
            
            Dictionary<string, object> changes = new Dictionary<string, object> 
            { 
                { "health", 75 },
                { "id", 42 },
                { "msg", "updated" }
            };
            SendObjUpdate(testObj, changes);

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(75, testObj.health, "Health not updated correctly");
            Assert.AreEqual(42, testObj.id, "ID not updated correctly");
            Assert.AreEqual("updated", testObj.msg, "Message not updated correctly");
        }


        [UnityTest]
        public IEnumerator HandleOwnershipChange()
        {
            yield return WaitConnection();

            // Simulate ownership change message
            NetMessage ownerMsg = new OwnershipMessage(testObj.NetObject.NetId, CLIENT_ID);
            host.Send(NetSerializer.Serialize(ownerMsg));

            // Wait for ownership to be updated
            float startTime = Time.time;
            while (testObj.NetObject.OwnerId != CLIENT_ID && Time.time - startTime < 1f)
            {
                yield return null;
            }

            Assert.AreEqual(CLIENT_ID, testObj.NetObject.OwnerId, "Ownership not updated correctly");
        }

        [TearDown]
        public void TearDown()
        {
            host.Stop();
            NetManager.StopNet();
            clientIds.Clear();
            
            var objects = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj.gameObject);
            }
        }

        
        private void SendObjUpdate(NetBehaviour netObj, Dictionary<string, object> changes)
        {
            NetMessage spw = new SyncMessage(netObj.NetID, 0, changes);
            host.Send(NetSerializer.Serialize(spw));
        }
        private IEnumerator WaitConnection()
        {
            clientIds.Add(0);
            yield return new WaitForSeconds(0.5f);
            NetMessage rcv = new ConnMessage(0, clientIds, new ServerInfo(){Address = "127.0.0.1", Port = NetManager.Port, MaxPlayers = 10});
            host.Send(NetSerializer.Serialize(rcv));
            yield return new WaitForSeconds(0.5f);
            SpawnMessage spw = new SpawnMessage(-1, testObj.name, Vector3.one, sceneId:testObj.GetComponent<SceneObjectId>().sceneId);
            spw.netObjectId = 0;
            rcv = spw;
            host.Send(NetSerializer.Serialize(rcv));
            yield return WaitValidate(typeof(SpawnMessage));
        }

        private IEnumerator WaitValidate(Type expectedType)
        {
            byte[] data = null;
            float startTime = Time.time;
            NetMessage msg = null;
            while (Time.time - startTime < 1f)
            {
                data = host.Receive();
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