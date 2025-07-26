using UnityEngine;

namespace IceCold.Interface
{
    public abstract class CoreConfig : ScriptableObject
    {
        public abstract string Key { get; }

        public static T GetConfig<T>(string key) where T : CoreConfig
        {
            var config = Resources.Load<T>(key);

            return config;
        }
    }
}