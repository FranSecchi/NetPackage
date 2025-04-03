using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetworkManager.NetPackage.Runtime.NetworkManager;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Runtime.NetPackage.Runtime.NetworkManager;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    public class StateSyncTests
    {
        private NetObject netObject;
        private TestObj testObj;
        private ITransport client;
        private int ids;
        private SyncMessage received;
        [SetUp]
        public void SetUp()
        {
            new GameObject().AddComponent<NetManager>();
            NetManager.StartHost();
            client = new UDPSolution();
            client.Setup(NetManager.Port, false);
            client.Start();
            
            testObj = new TestObj(30, 600, "hello");
            ids = 1;
            netObject = new NetObject( ids++, testObj);
        }

        [Test]
        public void RegisterObject()
        {
            Debug.Log(ids + ": " + netObject.ObjectId);
            Assert.IsTrue(StateManager.GetState(ids-1).TrackedSyncVars.Count != 0, "Snapshot is empty");
        }

        [Test]
        public void UpdateObject()
        {
            ObjectState state = StateManager.GetState(ids-1);
            testObj.health -= 100;
            Dictionary<int, Dictionary<string, object>> changes = state.Update();
            
            Assert.IsTrue(changes.Count != 0, "No changes");
        }

        [UnityTest]
        public IEnumerator SendUpdates()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
            Messager.RegisterHandler<SyncMessage>(OnReceived);
            
            testObj.health -= 100;
            ObjectState state = StateManager.GetState(ids-1).Clone();
            StateManager.SendUpdateStates();
            yield return new WaitForSeconds(0.5f);
            Messager.HandleMessage(client.Receive());
            
            Assert.IsNotNull(received, "Received is null");
            Assert.IsTrue(received.ObjectID == netObject.ObjectId, "Received object id is incorrect");
            Assert.IsTrue(state.ObjectIds.ContainsKey(received.ComponentId), "Received component id is incorrect: "+received.ComponentId);
        }

        [UnityTest]
        public IEnumerator ReceiveUpdates()
        {
            yield return new WaitForSeconds(0.2f);
            client.Connect("localhost");
            yield return new WaitForSeconds(0.2f);
            
            TestObj clone = testObj.Clone();
            ObjectState state = new ObjectState();
            state.Register(clone);
            clone.health -= 100;
            var changes = state.Update();
            foreach (var change in changes)
            {
                NetMessage msg = new SyncMessage(netObject.ObjectId, change.Key, change.Value);
                client.Send(NetSerializer.Serialize(msg));
            }
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(clone.health.Equals(testObj.health), "Received health update is incorrect");
            
        }
        [TearDown]
        public void TearDown()
        {
            client.Disconnect();
            NetManager.StopHosting();
            Messager.ClearHandlers();
            received = null;
        }

        private void OnReceived(SyncMessage obj)
        {
            Debug.Log("invoked");
            received = obj;
        }
    }
    // yield return new WaitForSeconds(0.2f);
    // client.Connect("localhost");
    // yield return new WaitForSeconds(0.2f);
    //
    // Dictionary<string, object> changes = new Dictionary<string, object>();
    // Dictionary<string, object> dict = new();
    // foreach (FieldInfo field in typeof(TestObj).GetFields(BindingFlags.Public | BindingFlags.Instance))
    // {
    //     dict[field.Name] = field.GetValue(testObj);
    // }
    public class TestObj 
    {
        [Sync]public int id;
        [Sync]public int health;
        [Sync]public string msg;

        public TestObj(int id, int i, string helloWorld)
        {
            this.id = id;
            health = i;
            msg = helloWorld;
        }

        public TestObj Clone()
        {
            return new TestObj(id, health, msg);
        }
    }
}
