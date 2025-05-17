using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;

namespace NetPackage.Tests
{
    public class TestSceneManager
    {
        private readonly string scenePath;
        private readonly string sceneName;
        private int originalSceneCount;
        private string[] originalScenes;

        public TestSceneManager(string sceneName)
        {
            this.sceneName = sceneName;
            this.scenePath = $"Packages/NetPackage/Tests/Resources/{sceneName}.unity";
        }

        public void SetUp()
        {
            originalSceneCount = EditorBuildSettings.scenes.Length;
            originalScenes = new string[originalSceneCount];
            for (int i = 0; i < originalSceneCount; i++)
            {
                originalScenes[i] = EditorBuildSettings.scenes[i].path;
            }

            bool sceneExists = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath)
                {
                    sceneExists = true;
                    break;
                }
            }

            if (!sceneExists)
            {
                var newScenes = new EditorBuildSettingsScene[originalSceneCount + 1];
                for (int i = 0; i < originalSceneCount; i++)
                {
                    newScenes[i] = EditorBuildSettings.scenes[i];
                }
                newScenes[originalSceneCount] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                
                EditorApplication.ExecuteMenuItem("File/Save Project");
                AssetDatabase.Refresh();
            }
        }

        public void TearDown()
        {
            if (originalScenes != null)
            {
                var scenes = new EditorBuildSettingsScene[originalSceneCount];
                for (int i = 0; i < originalSceneCount; i++)
                {
                    scenes[i] = new EditorBuildSettingsScene(originalScenes[i], true);
                }
                EditorBuildSettings.scenes = scenes;
                
                EditorApplication.ExecuteMenuItem("File/Save Project");
                AssetDatabase.Refresh();
            }
        }
    }
} 