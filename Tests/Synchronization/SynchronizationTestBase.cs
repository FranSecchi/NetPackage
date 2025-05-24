using System;
using UnityEngine.TestTools;
using System.Collections;
using NetPackage.Messages;
using NetPackage.Network;
using NetPackage.Serializer;
using NetPackage.Transport;
using NetPackage.Transport.UDP;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetPackage.Synchronization.Tests
{
    public abstract class SynchronizationTestBase
    {
        private NetPrefabRegistry prefabs;
        protected ITransport _server;
        protected ITransport _client;
        protected NetMessage received;
        protected string TEST_SCENE_NAME = "TestScene";
        protected const int CLIENT_ID = 0;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            new GameObject().AddComponent<NetManager>();
            RegisterPrefab();
            yield return SetUp();

        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            TEST_SCENE_NAME = "TestScene";
            NetManager.StopNet();
            StateManager.Clear();
            Messager.ClearHandlers();
            _client?.Stop();
            _server?.Stop();
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in objects)
            {
                GameObject.DestroyImmediate(obj);
            }
            received = null;
            yield return Teardown();
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSeconds(0.2f);
        }

        protected abstract IEnumerator SetUp();
        protected abstract IEnumerator Teardown();
        protected void StartClient(bool manager)
        {
            if(manager) NetManager.StartClient();
            else
            {
                _client = new UDPSolution();
                _client.Setup(NetManager.Port, false);
                _client.Start();
            }
        }

        protected void StartHost(bool manager)
        {
            if(manager) NetManager.StartHost();
            else
            {
                _server = new UDPSolution();
                _server.Setup(NetManager.Port, true,  new ServerInfo(){Address = "127.0.0.1", Port = NetManager.Port, MaxPlayers = 10});
                _server.Start();
                ITransport.OnClientConnected += OnClientConnected;
                ITransport.OnClientDisconnected += OnClientDisconnected;
            }
        }
        protected virtual void OnClientConnected(int id){}
        protected virtual void OnClientDisconnected(int id){}
        private void RegisterPrefab()
        {
            var prefab = Resources.Load<GameObject>("TestObj");
            prefabs = ScriptableObject.CreateInstance<NetPrefabRegistry>();
            prefabs.prefabs.Add(prefab);
            NetScene.RegisterPrefabs(prefabs.prefabs);
        }
        protected IEnumerator WaitValidate(Type expectedType)
        {
            byte[] data = null;
            float startTime = Time.time;
            NetMessage msg = null;
            while (Time.time - startTime < 1f)
            {
                data = _client.Receive();
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

        protected virtual IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            _client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
            NetManager.LoadScene(TEST_SCENE_NAME);
            yield return WaitValidate(typeof(SceneLoadMessage));
            
            SceneLoadMessage scnMsg = (SceneLoadMessage)received;
            Assert.AreEqual(TEST_SCENE_NAME, scnMsg.sceneName, "Wrong scene name");
            scnMsg.isLoaded = true; scnMsg.requesterId = CLIENT_ID;
            NetMessage answerMsg = scnMsg;
            _client.Send(NetSerializer.Serialize(answerMsg));
            yield return new WaitForSeconds(0.5f);
        }
        protected IEnumerator WaitSpawnSync(TestObj[] objs)
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
                _client.Send(NetSerializer.Serialize(received));
                yield return new WaitForSeconds(0.2f);
            }
        }
    }
} 