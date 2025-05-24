using System.Collections;
using NUnit.Framework;
using NetPackage.Network;
using NetPackage.Serializer;
using NetPackage.Messages;
using UnityEngine;
using UnityEngine.TestTools;

namespace NetPackage.Synchronization.Tests
{
    public class SceneSyncTests : SynchronizationTestBase
    {
        protected override IEnumerator SetUp()
        {
            StartHost(true);
            yield return new WaitForSeconds(0.2f);
            
            StartClient(false);
            yield return new WaitForSeconds(0.2f);
        }

        protected override IEnumerator Teardown()
        {
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneSynchronizationTest()
        {
            yield return WaitConnection();
            
            NetManager.LoadScene(TEST_SCENE_NAME);
            yield return new WaitForSeconds(0.5f);

            var hostObjects = GameObject.FindObjectsByType<TestObj>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(hostObjects.Length, 0, "No NetBehaviour objects found in host scene");
            yield return WaitValidate(typeof(SceneLoadMessage));
            
            SceneLoadMessage scnMsg = (SceneLoadMessage)received;
            Assert.AreEqual(TEST_SCENE_NAME, scnMsg.sceneName, "Wrong scene name");
            scnMsg.isLoaded = true; scnMsg.requesterId = CLIENT_ID;
            NetMessage answerMsg = scnMsg;
            _client.Send(NetSerializer.Serialize(answerMsg));
            yield return new WaitForSeconds(0.5f);
            
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg = (SpawnMessage)received;
            yield return WaitValidate(typeof(SpawnMessage));
            SpawnMessage spwMsg2 = (SpawnMessage)received;

            foreach (NetBehaviour hostObj in hostObjects)
            {
                var hostNetObj = hostObj.NetObject;
                Assert.IsNotNull(hostNetObj, "Host object missing NetObject component");
                SpawnMessage spw = hostNetObj.NetId != spwMsg.netObjectId ? spwMsg2 : spwMsg;
                
                Assert.AreEqual(hostObj.transform.position, spw.position, "Object positions don't match");
                Assert.AreEqual(hostNetObj.NetId, spw.netObjectId, "Object NetId doesn't match");
                Assert.AreEqual(hostNetObj.SceneId, spw.sceneId, "Object sceneId doesn't match");
                received = spw;
                _client.Send(NetSerializer.Serialize(received));
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

        protected override IEnumerator WaitConnection()
        {
            yield return new WaitForSeconds(0.2f);
            _client.Connect("localhost");
            yield return new WaitForSeconds(1f);
        }
    }
} 