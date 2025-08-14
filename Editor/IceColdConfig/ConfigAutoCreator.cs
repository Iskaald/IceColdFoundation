using System;
using System.Collections.Generic;
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
            EditorApplication.delayCall += UpdateModuleGroups;
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
                .Where(type => typeof(IceColdConfig).IsAssignableFrom(type) && !type.IsAbstract);

            var createdCount = 0;
            foreach (var type in configTypes)
            {
                var tempInstance = (IceColdConfig)ScriptableObject.CreateInstance(type);
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

        private static void UpdateModuleGroups()
        {
            // Find the LoggerConfig asset
            var loggerConfigGuids = AssetDatabase.FindAssets($"t:{nameof(LoggerConfig)}");
            if (loggerConfigGuids.Length == 0) return;
            var configPath = AssetDatabase.GUIDToAssetPath(loggerConfigGuids[0]);
            var config = AssetDatabase.LoadAssetAtPath<LoggerConfig>(configPath);
            
            // Find all module definition markers
            var moduleMarkerGuids = AssetDatabase.FindAssets($"t:{nameof(IceColdModuleDefinition)}");
            
            // all required path info
            var foundModules = new List<(string name, string projectPath, string absolutePath)>();

            foreach (var guid in moduleMarkerGuids)
            {
                var projectPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Get the absolute system path for the marker asset
                var absoluteMarkerPath = Path.GetFullPath(projectPath);

                // The module path is the directory containing the marker file
                var absoluteModulePath = Path.GetDirectoryName(absoluteMarkerPath);
                var projectModulePath = Path.GetDirectoryName(projectPath);

                // The group name is the name of that directory
                var moduleName = Path.GetFileName(projectModulePath);
                
                foundModules.Add((moduleName, projectModulePath, absoluteModulePath));
            }

            // Synchronize the config (pass the new data structure)
            var isDirty = SynchronizeGroups(config, foundModules);

            if (isDirty)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"LoggerConfig updated successfully. Found {foundModules.Count} modules.", config);
            }
            else
            {
                Debug.Log("LoggerConfig groups are already up-to-date.", config);
            }
        }

        private static bool SynchronizeGroups(LoggerConfig config, List<(string name, string projectPath, string absolutePath)> foundModules)
        {
            var hasChanged = false;
            // Use the projectPath as the unique identifier for a group
            var foundModuleProjectPaths = new HashSet<string>(foundModules.Select(m => m.projectPath));

            // Remove groups where the module folder/marker no longer exists
            var removedCount = config.groupSettings.RemoveAll(group => !foundModuleProjectPaths.Contains(group.ProjectPath));
            if (removedCount > 0)
            {
                hasChanged = true;
                Debug.Log($"Removed {removedCount} obsolete group(s) from LoggerConfig.");
            }
            
            var existingGroupProjectPaths = new HashSet<string>(config.groupSettings.Select(g => g.ProjectPath));

            // Add or update modules
            foreach (var (name, projectPath, absolutePath) in foundModules)
            {
                if (!existingGroupProjectPaths.Contains(projectPath))
                {
                    // Add new group
                    config.groupSettings.Add(new GroupLogFilterSettings(name, projectPath, absolutePath));
                    hasChanged = true;
                    Debug.Log($"Added new group '{name}' from path '{projectPath}' to LoggerConfig.");
                }
                else
                {
                    // Resynchronize the absolute path in case it changed
                    // (e.g., project moved to a different directory)
                    var existingGroup = config.groupSettings.First(g => g.ProjectPath == projectPath);
                    var normalizedNewAbsPath = absolutePath.Replace('\\', '/');
                    if (existingGroup.AbsolutePath != normalizedNewAbsPath)
                    {
                        existingGroup.AbsolutePath = normalizedNewAbsPath;
                        hasChanged = true;
                        Debug.Log($"Updated absolute path for group '{name}'.");
                    }
                }
            }

            if (hasChanged)
            {
                config.groupSettings = config.groupSettings.OrderBy(g => g.groupName).ToList();
            }

            return hasChanged;
        }
    }
}