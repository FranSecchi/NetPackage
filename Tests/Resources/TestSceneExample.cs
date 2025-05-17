using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace NetPackage.Tests
{
    public class TestSceneExample
    {
        private TestSceneManager sceneManager;
        private const string TestSceneName = "TestScene";

        [SetUp]
        public void SetUp()
        {
            sceneManager = new TestSceneManager(TestSceneName);
            sceneManager.SetUp();
        }

        [TearDown]
        public void TearDown()
        {
            sceneManager.TearDown();
        }

        [Test]
        public void YourTest()
        {
            // Your test code here
        }
    }
} 