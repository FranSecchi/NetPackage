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
        private NetMessage received;
        private const int CLIENT_ID = 0;
        private List<int> clientIds = new List<int>();
        private const string TEST_SCENE_NAME = "TestScene";

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
            
            yield return WaitConnection();
            NetMessage answerMsg =  new SceneLoadMessage(TEST_SCENE_NAME, -1);
            host.Send(NetSerializer.Serialize(answerMsg));
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator ReceiveStateUpdate()
        {
            var hostObjects = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(hostObjects.Length, 0, "No NetBehaviour objects found in host scene");
            foreach (NetBehaviour hostObj in hostObjects)
            {
                NetMessage msg = new SpawnMessage(-1, hostObj.gameObject.name, hostObj.transform.position, sceneId:hostObj.GetComponent<SceneObjectId>().sceneId);
                host.Send(NetSerializer.Serialize(msg));
                yield return new WaitForSeconds(0.2f);
            }
            Dictionary<string, object> changes = new Dictionary<string, object> 
            { 
                { "health", 75 },
                { "id", 42 },
                { "msg", "updated" }
            };
            SendObjUpdate(hostObjects[0], changes);

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(75, hostObjects[0].health, "Health not updated correctly");
            Assert.AreEqual(42, hostObjects[0].id, "ID not updated correctly");
            Assert.AreEqual("updated", hostObjects[0].msg, "Message not updated correctly");
        }


        [UnityTest]
        public IEnumerator HandleOwnershipChange()
        {
            var hostObjects = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(hostObjects.Length, 0, "No NetBehaviour objects found in host scene");
            foreach (NetBehaviour hostObj in hostObjects)
            {
                NetMessage msg = new SpawnMessage(-1, hostObj.gameObject.name, hostObj.transform.position, sceneId:hostObj.GetComponent<SceneObjectId>().sceneId);
                host.Send(NetSerializer.Serialize(msg));
                yield return new WaitForSeconds(0.2f);
            }
            
            foreach (var obj in hostObjects)
            {
                Assert.IsFalse(obj.isOwned, "Object not owned");
                Assert.IsFalse(obj.NetObject.Owned, "Object not owned");
                Assert.AreEqual(-1, obj.NetObject.OwnerId, "Wrong owner assigned");
            }
            
            NetMessage ownerMsg = new OwnershipMessage(hostObjects[0].NetObject.NetId, CLIENT_ID);
            host.Send(NetSerializer.Serialize(ownerMsg));

            // Wait for ownership to be updated
            float startTime = Time.time;
            while (hostObjects[0].NetObject.OwnerId != CLIENT_ID && Time.time - startTime < 1f)
            {
                yield return null;
            }
            var obj1 = hostObjects[0];
            Assert.IsFalse(obj1.isOwned, "Ownership not updated correctly");
            Assert.IsFalse(obj1.NetObject.Owned, "Ownership not updated correctly");
            Assert.AreEqual(CLIENT_ID, obj1.NetObject.OwnerId, "Ownership not updated correctly");
        }

        [TearDown]
        public void TearDown()
        {
            host.Stop();
            NetManager.StopNet();
            clientIds.Clear();
            received = null;
            var objects = GameObject.FindObjectsByType<NetManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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