using System;
using System.Collections;
using NUnit.Framework;
using Runtime;
using Runtime.NetPackage.Runtime.NetworkManager;
using NetPackage.Runtime.Serializer;
using NetPackage.Runtime.Messages;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class NetDestroyTests
    {
        private NetPrefabRegistry prefabs;
        private ITransport client;
        private NetMessage received;
        private const int CLIENT_ID = 0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            RegisterPrefab();
            
            SceneManager.LoadScene("TestScene");
            yield return new WaitForSeconds(0.5f);
            
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
            
            NetManager.Destroy(spawnedObj.NetObject.NetId);
            yield return new WaitForSeconds(0.2f);
            
            byte[] data = client.Receive();
            Assert.NotNull(data, "Client did not receive destroy message");
            
            NetMessage receivedMsg = NetSerializer.Deserialize<NetMessage>(data);
            Assert.IsTrue(receivedMsg is DestroyMessage, "Wrong message type received");
            
            DestroyMessage destroyMsg = (DestroyMessage)receivedMsg;
            Assert.AreEqual(spawnedObj.NetObject.NetId, destroyMsg.netObjectId, "Wrong object ID in destroy message");
            
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(0, objs.Length, "Object not destroyed");
        }

        [UnityTest]
        public IEnumerator ClientDestroyRequestTest()
        {
            yield return WaitConnection();
            
            Vector3 spawnPos = new Vector3(4, 5, 6);
            NetMessage clientSpawnMsg = new SpawnMessage(CLIENT_ID, "TestObj", spawnPos, own: true);
            client.Send(NetSerializer.Serialize(clientSpawnMsg));
            yield return new WaitForSeconds(0.5f);
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 1, "Object not spawned");
            
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
            
            // Verify object was destroyed
            objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, objs.Length, "Object not destroyed");
        }

        [UnityTest]
        public IEnumerator UnauthorizedDestroyTest()
        {
            yield return WaitConnection();
            
            Vector3 spawnPos = new Vector3(1, 2, 3);
            SpawnMessage hostSpawnMsg = new SpawnMessage(NetManager.ConnectionId(), "TestObj", spawnPos);
            NetScene.Instance.Spawn(hostSpawnMsg);
            
            yield return WaitValidate(typeof(SpawnMessage));
            client.Send(NetSerializer.Serialize(received));
            yield return new WaitForSeconds(0.2f);
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 1, "Object not spawned");
            
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
            Assert.Greater(objs.Length, 0, "Object was destroyed by unauthorized client");
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