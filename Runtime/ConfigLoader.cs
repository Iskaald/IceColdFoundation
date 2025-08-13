using UnityEngine;
using IceCold.Interface;

namespace IceCold
{
    public static class ConfigLoader
    {
        /// <summary>
        /// Gets the strongly-typed config data object for runtime use.
        /// It finds the corresponding ConfigNode asset in Resources and returns its data payload.
        /// </summary>
        /// <typeparam name="T">The type of the Config DATA (e.g., TestConfigData), NOT the Node.</typeparam>
        /// <param name="key">The key of the config asset to load.</param>
        /// <returns>The populated config data object, or a new default instance if not found.</returns>
        public static T Get<T>(string key) where T : new()
        {
            var node = Resources.Load<BaseConfigNode>(key);

            if (node == null)
            {
                Debug.LogWarning($"[ConfigLoader] Config asset with key '{key}' not found in Resources. " +
                                 "Returning a temporary instance with default values.");
                return new T();
            }

            // Get the data object and cast it to the requested type.
            var data = node.GetConfigData();
            if (data is T typedData)
            {
                return typedData;
            }
            
            Debug.LogError($"[ConfigLoader] Config asset '{key}' was found, but its data is not of the requested type '{typeof(T).Name}'. " +
                           $"It is of type '{data.GetType().Name}'.");
            return new T();
        }
    }
}