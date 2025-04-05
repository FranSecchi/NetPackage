using System.Collections.Generic;
using NUnit.Framework;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using UnityEngine;

namespace SynchronizationTest
{
    public class StateSyncTests
    {
        private NetObject netObject;
        private TestObj testObj;
        private int ids;
        
        [SetUp]
        public void SetUp()
        {
            
            testObj = new TestObj(30, 600, "hello");
            ids = 1;
            netObject = new NetObject( ids++, testObj);
        }

        [Test]
        public void RegisterObject()
        {
            Assert.IsTrue(StateManager.GetState(netObject.ObjectId).TrackedSyncVars.Count != 0, "Snapshot is empty");
        }

        [Test]
        public void UpdateObject()
        {
            ObjectState state = StateManager.GetState(ids-1);
            testObj.health -= 100;
            Dictionary<int, Dictionary<string, object>> changes = state.Update();
            
            Assert.IsTrue(changes.Count != 0, "No changes");
        }

        [TearDown]
        public void TearDown()
        {
            Messager.ClearHandlers();
        }

    }
    
    public class TestObj : MonoBehaviour
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
