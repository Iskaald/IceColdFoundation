using UnityEngine;

namespace IceColdCore.Interface
{
    public abstract class CoreConfig : ScriptableObject
    {
        public abstract string Key { get; }
    }
}