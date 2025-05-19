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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace SynchronizationTest
{
    public class SceneSyncTests
    {
        private ITransport client;
        private const int CLIENT_ID = 0;
        private const string TEST_SCENE_NAME = "TestScene";
        private NetMessage received;


        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Setup networking
            var managerObj = new GameObject();
            var manager = managerObj.AddComponent<NetManager>();
            NetManager.StartHost();
            
            yield return new WaitForSeconds(0.2f);
            
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneSynchronizationTest()
        {
            // Connect client to host
            yield return WaitConnection();
            
            NetManager.LoadScene(TEST_SCENE_NAME);
            yield return new WaitForSeconds(0.5f); // Wait for scene to load

            // Verify that the scene was loaded in the host
            var hostObjects = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(hostObjects.Length, 0, "No NetBehaviour objects found in host scene");
            Debug.Log($"Count; {hostObjects.Length} | {hostObjects[0].GetComponent<SceneObjectId>().sceneId} | {hostObjects[1].GetComponent<SceneObjectId>().sceneId}");
            // Wait for scene synchronization
            yield return WaitValidate(typeof(SceneLoadMessage));
            
            SceneLoadMessage scnMsg = (SceneLoadMessage)received;
            Assert.AreEqual(TEST_SCENE_NAME, scnMsg.sceneName, "Wrong scene name");
            scnMsg.isLoaded = true; scnMsg.requesterId = CLIENT_ID;
            NetMessage answerMsg = scnMsg;
            client.Send(NetSerializer.Serialize(answerMsg));
            yield return new WaitForSeconds(0.5f); // Wait for scene to load
            
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg = (SpawnMessage)received;
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg2 = (SpawnMessage)received;

            foreach (NetBehaviour hostObj in hostObjects)
            {
                var hostNetObj = hostObj.NetObject;
                Assert.IsNotNull(hostNetObj, "Host object missing NetObject component");
                SpawnMessage spw = hostNetObj.NetId != spwMsg.netObjectId ? spwMsg2 : spwMsg;
                
                // Verify object properties are synchronized
                Assert.AreEqual(hostObj.transform.position, spw.position, "Object positions don't match");
                Assert.AreEqual(hostNetObj.NetId, spw.netObjectId, "Object NetId doesn't match");
                Assert.AreEqual(hostNetObj.SceneId, spw.sceneId, "Object sceneId doesn't match");
                received = spw;
                client.Send(NetSerializer.Serialize(received));
                yield return new WaitForSeconds(0.2f);
            }
            
            TestObj testComponent = hostObjects[0];
            testComponent.Set(42, 100, "test");
            StateManager.SendUpdateStates();
            
            yield return WaitValidate(typeof(SyncMessage));

            SyncMessage syncMsg = (SyncMessage)received;
            Assert.AreEqual(testComponent.NetObject.NetId, syncMsg.ObjectID, "Wrong object ID in sync message");
            Assert.Greater(syncMsg.changedValues.Count, 0, "No state changes in sync message");
        }

        private IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return new WaitForSeconds(1f);
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
    }
} 