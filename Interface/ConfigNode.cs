using UnityEngine;

namespace IceCold.Interface
{
    /// <summary>
    /// The generic base class for a configuration asset.
    /// It holds a strongly-typed data object (T) which contains the actual config values.
    /// </summary>
    /// <typeparam name="T">The type of the plain C# class that holds the configuration data.</typeparam>
    public abstract class ConfigNode<T> : BaseConfigNode where T : new()
    {
        [Header("Configuration Data")]
        [SerializeField]
        private T data = new();

        /// <summary>
        /// Provides public, strongly-typed access to the configuration data.
        /// </summary>
        public T Data => data;

        /// <summary>
        /// Implementation of the non-generic getter for the runtime loader.
        /// </summary>
        public override object GetConfigData() => data;
    }
}