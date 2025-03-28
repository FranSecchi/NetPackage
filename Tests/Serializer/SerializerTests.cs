using System.Collections.Generic;
using MessagePack;
using NUnit.Framework;
using Serializer.NetPackage.Runtime.Serializer;
using UnityEngine;
using UnityEngine.TestTools;

namespace SerializerTest.NetPackage.Tests.Serializer
{
    public class SerializerTests
    {
        private object objectTest;
        [SetUp]
        public void Setup()
        {
            NetSerializer._Serializer = new MPSerializer();
        }

        [Test]
        public void StringTest()
        {
            objectTest = "Hello World";
            byte[] pck = NetSerializer.Serialize(objectTest);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsTrue(objectTest.Equals(NetSerializer.Deserialize<object>(pck)), "Serialized object is incorrect");
        }
        [Test]
        public void IntTest()
        {
            objectTest = 5678;
            byte[] pck = NetSerializer.Serialize(objectTest);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsTrue(objectTest.Equals(NetSerializer.Deserialize<object>(pck)), "Serialized object is incorrect");
        }
        [Test]
        public void ObjectTest()
        {
            MessageTest msg = new MessageTest(456, "Message");
            byte[] pck = NetSerializer.Serialize(msg);
            MessageTest msg2 = NetSerializer.Deserialize<MessageTest>(pck);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsTrue(msg.health.Equals(msg2.health) && msg.msg.Equals(msg2.msg), "Serialized object is incorrect");
        }
        [Test]
        public void ListTest()
        {
            List<int> list = new List<int>();
            list.Add(5678);
            byte[] pck = NetSerializer.Serialize(list);
            List<int> list2 = NetSerializer.Deserialize<List<int>>(pck);
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsNotNull(list2, "Deserialized list is null");
            Assert.IsTrue(list2.Count == list.Count, "Deserialized list count is incorrect");
            Assert.IsTrue(list2.Contains(5678), "Deserialized object is incorrect");
        }
        [Test]
        public void ObjectListTest()
        {
            List<MessageTest> list = new List<MessageTest>();
            list.Add(new MessageTest(5678, "Message")
            );
            byte[] pck = NetSerializer.Serialize(list);
            List<MessageTest> list2 = NetSerializer.Deserialize<List<MessageTest>>(pck);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsNotNull(list2, "Deserialized list is null");
            Assert.IsTrue(list2.Count == list.Count, "Deserialized list count is incorrect");
            Assert.IsTrue(list2[0].msg.Equals(list[0].msg), "Deserialized object is incorrect");
        }
        [Test]
        public void NestedListTest()
        {
            List<MessageTest> list = new List<MessageTest>();
            list.Add(new MessageTest(5678, "Message", new List<int>(){0,1,2}));
            
            byte[] pck = NetSerializer.Serialize(list);
            List<MessageTest> list2 = NetSerializer.Deserialize<List<MessageTest>>(pck);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsNotNull(list2, "Deserialized list is null");
            Assert.IsTrue(list2.Count == list.Count, "Deserialized list count is incorrect");
            Assert.IsTrue(list2[0].list[0].Equals(list[0].list[0]), "Deserialized object is incorrect");
        }
        [Test]
        public void DictionaryTest()
        {
            Dictionary<int,MessageTest> dic = new Dictionary<int,MessageTest>();
            dic[0] = new MessageTest(5678, "Message", new List<int>(){0,1,2});
            
            byte[] pck = NetSerializer.Serialize(dic);
            Dictionary<int,MessageTest> dic2 = NetSerializer.Deserialize< Dictionary<int,MessageTest>>(pck);
            
            Assert.IsNotNull(pck, "Serialized object is null");
            Assert.IsNotNull(dic2, "Deserialized list is null");
            Assert.IsTrue(dic2.Count == dic.Count, "Deserialized list count is incorrect");
            Assert.IsTrue(dic[0].msg.Equals(dic2[0].msg), "Deserialized object is incorrect");
        }
    }
    [MessagePackObject]
    public class MessageTest
    {
        [Key(0)] public int health;
        [Key(1)] public string msg;
        [Key(2)] public List<int> list;

        public MessageTest() { }

        public MessageTest(int health, string msg, List<int> list = null)
        {
            this.health = health;
            this.msg = msg;
            this.list = list;
        }
    }
}
