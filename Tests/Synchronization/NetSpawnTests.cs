using System;
using System.Collections;
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
    public class NetSpawnTests
    {
        private NetPrefabRegistry prefabs;
        private ITransport client;
        private const string TEST_SCENE_NAME = "TestScene";
        private NetMessage received;
        private const int CLIENT_ID = 0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            RegisterPrefab();
            
            yield return new WaitForSeconds(0.2f);
            
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnSynchronizationTest()
        {
            yield return WaitConnection();
            yield return WaitSpawnSync();
            
            Vector3 spawnPos = new Vector3(1, 2, 3);
            SpawnMessage hostSpawnMsg = new SpawnMessage(NetManager.ConnectionId(), "TestObj", spawnPos);
            NetScene.Spawn(hostSpawnMsg);
            
            yield return new WaitForSeconds(0.2f);
            
            byte[] data = client.Receive();
            Assert.NotNull(data, "Client did not receive spawn message");
            
            NetMessage receivedMsg = NetSerializer.Deserialize<NetMessage>(data);
            Assert.IsTrue(receivedMsg is SpawnMessage, "Wrong message type received");
            
            SpawnMessage spawnMsg = (SpawnMessage)receivedMsg;
            Assert.AreEqual(spawnPos, spawnMsg.position, "Wrong spawn position");
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 2, "Object not spawned");
            
            bool found = false;
            foreach (var obj in objs)
            {
                if (Vector3.Distance(obj.transform.position, spawnPos) < 0.01f)
                {
                    found = true;
                    Assert.NotNull(obj.NetObject, "NetObject not assigned");
                    
                    ObjectState state = StateManager.GetState(obj.NetObject.NetId);
                    Assert.NotNull(state, "Object state not registered");
                    break;
                }
            }
            Assert.IsTrue(found, "Spawned object not found at correct position");
        }

        [UnityTest]
        public IEnumerator ClientSpawnRequestTest()
        {
            yield return WaitConnection();
            yield return WaitSpawnSync();
            
            int initialCount = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            
            Vector3 spawnPos = new Vector3(4, 5, 6);
            NetMessage clientSpawnMsg = new SpawnMessage(CLIENT_ID, "TestObj", spawnPos, owner: CLIENT_ID);
            client.Send(NetSerializer.Serialize(clientSpawnMsg));
            yield return new WaitForSeconds(0.5f);
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(initialCount + 1, objs.Length, "Object not spawned");

            yield return WaitValidate(typeof(SpawnMessage));
            
            bool found = false;
            foreach (var obj in objs)
            {
                if (Vector3.Distance(obj.transform.position, spawnPos) < 0.01f)
                {
                    found = true;
                    Assert.NotNull(obj.NetObject, "NetObject not assigned");
                    Assert.IsFalse(obj.isOwned, "Object not owned");
                    Assert.IsFalse(obj.NetObject.Owned, "Object not owned");
                    Assert.AreEqual(CLIENT_ID, obj.NetObject.OwnerId, "Wrong owner assigned");
                    
                    ObjectState state = StateManager.GetState(obj.NetObject.NetId);
                    Assert.NotNull(state, "Object state not registered");
                }
                else
                {
                    Assert.IsTrue(obj.isOwned, "Object not owned");
                    Assert.IsTrue(obj.NetObject.Owned, "Object not owned");
                    Assert.AreEqual(NetManager.ConnectionId(), obj.NetObject.OwnerId, "Object not owned");
                }
            }
            Assert.IsTrue(found, "Spawned object not found at correct position");
        }

        [UnityTest]
        public IEnumerator SceneObjectSpawnTest()
        {
            yield return WaitConnection();
            yield return WaitSpawnSync();
            SpawnMessage spawnMsg = (SpawnMessage)received;
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objs)
            {
                Assert.IsNotEmpty(spawnMsg.sceneId, "Scene ID not set");
                if(obj.NetID != spawnMsg.netObjectId) continue;
                ObjectState state = StateManager.GetState(obj.NetObject.NetId);
                Assert.NotNull(state, "Scene object state not registered");
                Assert.AreEqual(obj.name, spawnMsg.prefabName, "Wrong prefab name");
                Assert.AreEqual(obj.transform.position, spawnMsg.position, $"Wrong position {spawnMsg.position}");
            }
        }
        
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            client.Stop();
            NetManager.StopNet();
            yield return new WaitForSeconds(0.2f);
            Messager.ClearHandlers();
            StateManager.Clear();
            
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj);
            }
            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
            NetManager.LoadScene(TEST_SCENE_NAME);
            yield return WaitValidate(typeof(SceneLoadMessage));
            
            SceneLoadMessage scnMsg = (SceneLoadMessage)received;
            Assert.AreEqual(TEST_SCENE_NAME, scnMsg.sceneName, "Wrong scene name");
            scnMsg.isLoaded = true; scnMsg.requesterId = CLIENT_ID;
            NetMessage answerMsg = scnMsg;
            client.Send(NetSerializer.Serialize(answerMsg));
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator WaitSpawnSync()
        {
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 0, "Object not spawned");
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg = (SpawnMessage)received;
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg2 = (SpawnMessage)received;
            
            foreach (NetBehaviour hostObj in objs)
            {
                var hostNetObj = hostObj.NetObject;
                SpawnMessage spw = hostNetObj.NetId != spwMsg.netObjectId ? spwMsg2 : spwMsg;
                received = spw;
                client.Send(NetSerializer.Serialize(received));
                yield return new WaitForSeconds(0.2f);
            }
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
            NetScene.RegisterPrefabs(prefabs.prefabs);
        }
    }
} 