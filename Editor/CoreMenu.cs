using IceColdCore.Interface;
using UnityEditor;
using UnityEngine;

namespace IceColdCore.Editor
{
    public partial class CoreMenu
    {
        private static T FindConfigAsset<T>() where T : CoreConfig
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            var newConfig = CreateConfig<T>(typeof(T).Name);
            
            return newConfig;
        }
        
        private static T CreateConfig<T>(string key) where T : CoreConfig
        {
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
        }
    }
}