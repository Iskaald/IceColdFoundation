using IceColdCore.Interface;
using UnityEngine;
using UnityEditor;

namespace IceColdCore
{
    public static class ConfigLoader
    {
        public static T GetConfig<T>(string key) where T : CoreConfig
        {
            var config = Resources.Load<T>(key);
            if (config == null)
            {
#if !UNITY_EDITOR
                CoreLogger.Log($"Failed to create config {key}");
                return null;
#endif
                config = CreateConfig<T>(key);
            }

            return config;
        }
        
        private static T CreateConfig<T>(string key) where T : CoreConfig
        {
#if UNITY_EDITOR
            var config = ScriptableObject.CreateInstance<T>();

            const string parent = "Assets/Core/Settings";
            var folderPath = $"{parent}/Resources";
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder("Assets/Core", "Settings");
                }
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets/Core/Settings", "Resources");
                
                AssetDatabase.Refresh();
            }
            
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{key}.asset");
            AssetDatabase.CreateAsset(config,assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return config;
#else
            return null;
#endif
        }
    }
}