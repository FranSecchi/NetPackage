using NetPackage.Runtime.Synchronization;
using UnityEngine;

namespace SynchronizationTest
{
    public class TestObj : NetBehaviour
    {
        [Sync]public int id;
        [Sync]public int health;
        [Sync]public string msg;

        public void Set(int id, int health, string msg)
        {
            this.id = id;
            this.health = health;
            this.msg = msg;
        }
        public TestObj Clone()
        {
            TestObj test =  new GameObject().AddComponent<TestObj>();
            test.id = id;
            test.health = health;
            test.msg = msg;
            return test;
        }
    }
}
