using System;
using System.Collections;
using NUnit.Framework;
using NetPackage.Runtime.NetworkManager;
using NetPackage.Runtime.Serializer;
using NetPackage.Runtime.Messages;
using NetPackage.Runtime.Synchronization;
using NetPackage.Tests;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class NetSpawnTests
    {
        private TestSceneManager sceneManager;
        private NetPrefabRegistry prefabs;
        private ITransport client;
        private NetMessage received;
        private const int CLIENT_ID = 0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            sceneManager = new TestSceneManager("TestScene");
            sceneManager.SetUp();
            yield return new WaitForSeconds(0.2f);
            
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            RegisterPrefab();
            
            SceneManager.LoadScene("TestScene");
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
            
            Vector3 spawnPos = new Vector3(1, 2, 3);
            SpawnMessage hostSpawnMsg = new SpawnMessage(NetManager.ConnectionId(), "TestObj", spawnPos);
            NetScene.Instance.Spawn(hostSpawnMsg);
            
            yield return new WaitForSeconds(0.2f);
            
            byte[] data = client.Receive();
            Assert.NotNull(data, "Client did not receive spawn message");
            
            NetMessage receivedMsg = NetSerializer.Deserialize<NetMessage>(data);
            Assert.IsTrue(receivedMsg is SpawnMessage, "Wrong message type received");
            
            SpawnMessage spawnMsg = (SpawnMessage)receivedMsg;
            Assert.AreEqual(spawnPos, spawnMsg.position, "Wrong spawn position");
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(objs.Length, 0, "Object not spawned");
            
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
            
            int initialCount = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            
            Vector3 spawnPos = new Vector3(4, 5, 6);
            NetMessage clientSpawnMsg = new SpawnMessage(CLIENT_ID, "TestObj", spawnPos, own: true);
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
                    Assert.AreEqual(CLIENT_ID, obj.NetObject.OwnerId, "Wrong owner assigned");
                    
                    ObjectState state = StateManager.GetState(obj.NetObject.NetId);
                    Assert.NotNull(state, "Object state not registered");
                    break;
                }
            }
            Assert.IsTrue(found, "Spawned object not found at correct position");
        }

        [UnityTest]
        public IEnumerator SceneObjectSpawnTest()
        {
            yield return WaitConnection();
            SpawnMessage spawnMsg = (SpawnMessage)received;
            Assert.GreaterOrEqual(spawnMsg.sceneId, 0, "Scene ID not set");
            
            var objs = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ObjectState state = StateManager.GetState(objs[0].NetObject.NetId);
            Assert.NotNull(state, "Scene object state not registered");
            Assert.AreEqual(objs[0].name, spawnMsg.prefabName, "Wrong prefab name");
            Assert.AreEqual(objs[0].transform.position, spawnMsg.position, $"Wrong position {spawnMsg.position}");
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