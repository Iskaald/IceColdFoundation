using UnityEngine;

namespace IceCold.Editor
{
    /// <summary>
    /// Place this asset in the root folder of a module/package 
    /// to make it discoverable by the IceCold Logger as a logging group.
    /// The name of the folder will be used as the group name.
    /// </summary>
    [CreateAssetMenu(fileName = "IceColdModuleDefinition", menuName = "IceCold/Setup/Create Module Definition", order = 101)]
    public class IceColdModuleDefinition : ScriptableObject
    {
        [Tooltip("Optional description for this module.")]
        public string description;
    }
}