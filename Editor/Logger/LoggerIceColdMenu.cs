using IceCold.Interface;
using UnityEditor;

namespace IceCold.Editor
{
    public class LoggerIceColdMenu : IceColdMenu
    {
        [MenuItem("IceCold/Logger/Config", priority = 2)]
        private static void SelectLoggerConfig()
        {
            var config = FindConfigAsset<LoggerConfig>();
            
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }
    }
}