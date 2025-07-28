using IceCold.Interface;
using UnityEditor;
using UnityEngine;

namespace IceCold.Editor
{
    public class CoreMenu
    {
        protected static T FindConfigAsset<T>() where T : CoreConfig
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

            const string parent = "Assets/IceCold/Settings";
            const string subfolder = "Resources";
            var folderPath = $"{parent}/{subfolder}";

            if (!AssetDatabase.IsValidFolder(parent))
            {
                if (!AssetDatabase.IsValidFolder("Assets/IceCold"))
                {
                    AssetDatabase.CreateFolder("Assets", "IceCold");
                }
                AssetDatabase.CreateFolder("Assets/IceCold", "Settings");
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, subfolder);
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{key}.asset");

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return config;
        }
    }
}