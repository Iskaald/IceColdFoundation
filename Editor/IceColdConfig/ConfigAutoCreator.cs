using System;
using System.IO;
using System.Linq;
using IceCold.Interface;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IceCold.Editor
{
    [InitializeOnLoad]
    public static class ConfigAutoCreator
    {
        private const string SetupFlagKey = "com.icecold.foundation.run_config_setup";

        static ConfigAutoCreator()
        {
            EditorApplication.delayCall += CheckForConfigSetupRequest;
        }

        private static void CheckForConfigSetupRequest()
        {
            if (EditorPrefs.GetBool(SetupFlagKey, false))
            {
                EditorPrefs.DeleteKey(SetupFlagKey);
                
                Debug.Log("Installer setup flag detected. Searching for and creating missing configs...");
                CreateAllMissingConfigs();
            }
        }

        [MenuItem("IceCold/Setup/Create Missing Configs", false, 100)]
        public static void CreateAllMissingConfigs()
        {
            var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(BaseConfigNode).IsAssignableFrom(type) && !type.IsAbstract);

            var createdCount = 0;
            foreach (var type in configTypes)
            {
                var tempInstance = (BaseConfigNode)ScriptableObject.CreateInstance(type);
                var key = tempInstance.Key;
                Object.DestroyImmediate(tempInstance);

                if (string.IsNullOrEmpty(key)) continue;

                var existingConfig = Resources.Load(key);
                if (existingConfig == null)
                {
                    Debug.Log($"Config for '{type.Name}' not found. Creating asset at key: {key}");
                    CreateConfigAsset(type, key);
                    createdCount++;
                }
            }

            if (createdCount > 0)
            {
                Debug.Log($"Successfully created {createdCount} new config asset(s).");
            }
            else
            {
                Debug.Log("All configs already exist. No new assets created.");
            }
        }

        private static void CreateConfigAsset(Type configType, string key)
        {
            var config = ScriptableObject.CreateInstance(configType);

            const string folderPath = "Assets/IceCold/Settings/Resources";
            
            Directory.CreateDirectory(folderPath);

            var assetPath = Path.Combine(folderPath, $"{key}.asset");
            
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(config, uniqueAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}