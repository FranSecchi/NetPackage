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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class NetDestroyTests
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
        public IEnumerator HostDestroySynchronizationTest()
        {
            yield return WaitConnection();
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 0, "Object not spawned");

            TestObj spawnedObj = objs[0];

            yield return WaitSpawnSync(objs);

            NetManager.Destroy(spawnedObj.NetObject.NetId);
            yield return WaitValidate(typeof(DestroyMessage));
            DestroyMessage destroyMsg = (DestroyMessage)received;
            
            Assert.AreEqual(spawnedObj.NetObject.NetId, destroyMsg.netObjectId, "Wrong object ID in destroy message");
            
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Object not destroyed");
        }


        [UnityTest]
        public IEnumerator ClientDestroyRequestTest()
        {
            yield return WaitConnection();
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 1, "Object not spawned");
            
            yield return WaitSpawnSync(objs);
            
            Vector3 spawnPos = new Vector3(4, 5, 6);
            NetMessage clientSpawnMsg = new SpawnMessage(CLIENT_ID, "TestObj", spawnPos, CLIENT_ID);
            client.Send(NetSerializer.Serialize(clientSpawnMsg));
            yield return new WaitForSeconds(0.5f);
            
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 2, "Object not spawned");
            
            TestObj spawnedObj = null;
            foreach (var obj in objs)
            {
                if (Vector3.Distance(obj.transform.position, spawnPos) < 0.01f)
                {
                    spawnedObj = obj;
                    break;
                }
            }
            Assert.NotNull(spawnedObj, "Spawned object not found");
            Assert.IsFalse(spawnedObj.isOwned, "Object not owned");
            Assert.IsFalse(spawnedObj.NetObject.Owned, "Object not owned");
            Assert.AreEqual(CLIENT_ID, spawnedObj.NetObject.OwnerId, "Object not owned");
            
            NetMessage clientDestroyMsg = new DestroyMessage(spawnedObj.NetObject.NetId, CLIENT_ID);
            client.Send(NetSerializer.Serialize(clientDestroyMsg));
            yield return new WaitForSeconds(0.5f);
            
            // Verify object was destroyed
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(2, objs.Length, "Object not destroyed");
        }

        [UnityTest]
        public IEnumerator UnauthorizedDestroyTest()
        {
            yield return WaitConnection();
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 1, "Object not spawned");
            yield return WaitSpawnSync(objs);
            
            Vector3 spawnPos = new Vector3(1, 2, 3);
            SpawnMessage hostSpawnMsg = new SpawnMessage(NetManager.ConnectionId(), "TestObj", spawnPos);
            NetScene.Spawn(hostSpawnMsg);
            
            yield return WaitValidate(typeof(SpawnMessage));
            client.Send(NetSerializer.Serialize(received));
            yield return new WaitForSeconds(0.2f);
            
            
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 2, "Object not spawned");
            TestObj spawnedObj = null;
            foreach (var obj in objs)
            {
                if (Vector3.Distance(obj.transform.position, spawnPos) < 0.01f)
                {
                    spawnedObj = obj;
                    break;
                }
            }
            Assert.NotNull(spawnedObj, "Spawned object not found");
            
            NetMessage clientDestroyMsg = new DestroyMessage(spawnedObj.NetObject.NetId, CLIENT_ID);
            client.Send(NetSerializer.Serialize(clientDestroyMsg));
            yield return new WaitForSeconds(0.5f);
            
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 1, "Object was destroyed by unauthorized client");
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

        private IEnumerator WaitSpawnSync(TestObj[] objs)
        {
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