using System.Collections;
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
    public class ServerState
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

        [UnityTest]
        public IEnumerator Server_Spawn()
        {
            yield return WaitConnection();
            
            client.Send(NetSerializer.Serialize(new SpawnMessage()));
        }
        
        [UnityTest]
        public IEnumerator Server_SendUpdates()
        {
            yield return WaitConnection();
            Messager.RegisterHandler<SyncMessage>(OnReceived);
            
            testObj.health -= 100;
            ObjectState state = StateManager.GetState(netObject.NetId).Clone();
            
            StateManager.SendUpdateStates();
            yield return new WaitForSeconds(0.5f);
            
            NetMessage msg = NetSerializer.Deserialize<NetMessage>(client.Receive());
            Messager.HandleMessage(msg);
            
            Assert.IsNotNull(received, "Received is null");
            Assert.IsTrue(received.ObjectID == netObject.NetId, "Received object id is incorrect");
            Assert.IsTrue(state.ObjectIds.ContainsKey(received.ComponentId), "Received component id is incorrect: "+received.ComponentId);
        }


        [UnityTest]
        public IEnumerator Server_ReceiveUpdates()
        {
            yield return WaitConnection();
            
            TestObj clone = testObj.Clone();
            ObjectState state = new ObjectState();
            state.Register(clone);
            clone.health -= 100;
            
            var changes = state.Update();
            foreach (var change in changes)
            {
                NetMessage msg = new SyncMessage(netObject.NetId, change.Key, change.Value);
                client.Send(NetSerializer.Serialize(msg));
            }
            yield return new WaitForSeconds(0.5f);
            
            Assert.IsTrue(clone.health.Equals(testObj.health), "Received health update is incorrect");
        }
        
        [UnityTest]
        public IEnumerator Server_MultipleClients_Sync()
        {
            yield return WaitConnection();
            ITransport client2 = new UDPSolution();
            client2.Setup(NetManager.Port, false);
            client2.Start();
            yield return new WaitForSeconds(0.5f);
            client2.Connect("localhost");
            yield return new WaitForSeconds(0.5f);
        
            testObj.health -= 50;
            StateManager.SendUpdateStates();
            yield return new WaitForSeconds(0.5f);
        
            Assert.IsTrue(client.Receive() != null, "Client 1 did not receive update");
            Assert.IsTrue(client2.Receive() != null, "Client 2 did not receive update");
        
            client2.Stop();
        }
    
        [UnityTest]
        public IEnumerator Server_Desync_Recovery()
        {
            yield return WaitConnection();
        
            testObj.health -= 200;
            StateManager.SendUpdateStates();
            yield return new WaitForSeconds(0.5f);
        
            // Simulate connection loss
            client.Disconnect();
            yield return new WaitForSeconds(1f);
        
            // Reconnect and check state recovery
            client.Connect("localhost");
            yield return new WaitForSeconds(0.5f);
        
            Assert.IsTrue(testObj.health == 400, "State recovery failed");
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
        private void OnReceived(SyncMessage obj)
        {
            received = obj;
        }
    }
}
