using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Runtime;
using Runtime.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class ServerSpawning
    {
        private NetObject netObject;
        private TestObj testObj;
        private ITransport client;
        private NetMessage received;
        private NetPrefabRegistry prefabs;
        
        [SetUp]
        public void SetUp()
        {
            new GameObject().AddComponent<NetManager>();
            NetManager.StartHost();
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            
        }
        
        [UnityTest]
        public IEnumerator HandleSpawnRequest()
        {
            RegisterPrefab();
            yield return WaitConnection();
            
            Vector3 pos = new Vector3(1,2,3);
            NetMessage msg = new SpawnMessage(1, "TestObj", pos);
            client.Send(NetSerializer.Serialize(msg));
            yield return new WaitForSeconds(0.5f);

            var objs = GameObject.FindObjectsByType<NetBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            Assert.AreEqual(1, objs.Length, "No objects found");
            Assert.AreEqual(0, objs[0].NetObject.NetId, "No correct name");
            Assert.AreEqual(pos, objs[0].transform.position, "Wrong position");
        }
        [UnityTest]
        public IEnumerator SpawnSceneObjects()
        {
            yield return WaitConnection();
            Vector3 pos = new Vector3(1,2,3);
            GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>("TestObj"), pos,Quaternion.identity);
            
            yield return new WaitForSeconds(0.5f);

            NetMessage msg = NetSerializer.Deserialize<NetMessage>(client.Receive());
            if (msg.GetType() == typeof(SpawnMessage))
            {  
                SpawnMessage m = (SpawnMessage)msg;
                Assert.AreEqual(obj.GetComponent<SceneObjectId>().sceneId, m.sceneId, "Wrong scene id");
                Assert.AreEqual(pos, m.position, "No correct position");
                Assert.AreEqual(obj.name, m.prefabName, "Wrong name");
            }
            else Assert.Fail("Wrong message type: " + msg.GetType());
        }
        [TearDown]
        public void TearDown()
        {
            NetManager.StopHosting();
            client.Stop();
            Messager.ClearHandlers();
            received = null;
        }
        
        private IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
        }
        private void RegisterPrefab()
        {
            var prefab = Resources.Load<GameObject>("TestObj");
            prefabs = ScriptableObject.CreateInstance<NetPrefabRegistry>();
            prefabs.prefabs.Add(prefab);
            NetScene.Instance = new NetScene();
            NetScene.Instance.RegisterPrefabs(prefabs.prefabs);
        }
    }
}
