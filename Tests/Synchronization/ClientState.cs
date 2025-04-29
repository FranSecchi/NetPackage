using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Runtime.NetPackage.Runtime.NetworkManager;
using Runtime.NetPackage.Runtime.Synchronization;
using Serializer.NetPackage.Runtime.Serializer;
using Synchronization.NetPackage.Runtime.Synchronization;
using Transport.NetPackage.Runtime.Transport;
using Transport.NetPackage.Runtime.Transport.UDP;
using UnityEngine;
using UnityEngine.TestTools;

namespace SynchronizationTest
{
    
    public class ClientState 
    {
        private NetObject netObject;
        private TestObj testObj;
        private ITransport server;
        private int ids;
        private SyncMessage received;

        [SetUp]
        public void Setup()
        {
            server = new UDPSolution();
            server.Setup(NetManager.Port, true);
            server.Start();
            
            new GameObject().AddComponent<NetManager>();
            NetManager.StartClient();
            
            testObj = new TestObj(30, 600, "hello");
            ids = 1;
            netObject = new NetObject( ids++, testObj);
        }

        [UnityTest]
        public IEnumerator SpawnRPC()
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        [UnityTest]
        public IEnumerator Client_SendUpdates()
        {
            yield return new WaitForSeconds(0.2f);
            
            testObj.health -= 100;
        }
        
        [TearDown]
        public void TearDown()
        {
            server.Stop();
            NetManager.StopClient();
        }
    }
}
