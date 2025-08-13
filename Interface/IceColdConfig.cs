using UnityEngine;

namespace IceCold.Interface
{
    public abstract class IceColdConfig : ScriptableObject
    {
        /// <summary>
        /// Gets the config asset from Resources. If not found, creates a temporary in-memory instance.
        /// </summary>
        public static T GetConfig<T>(string key) where T : IceColdConfig
        {
            var config = Resources.Load<T>(key);

            if (config == null)
            {
                Debug.LogWarning($"Config asset '{key}.asset' not found in Resources. " +
                                 "Creating a temporary in-memory instance with default values. This will NOT be saved.");
                
                config = CreateInstance<T>();
            }
            
            return config;
        }
    }
}