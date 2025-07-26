using IceCold.Interface;
using UnityEditor;

namespace IceCold.Editor
{
    public class LoggerCoreMenu : CoreMenu
    {
        [MenuItem("IceCold/Logger/Config", priority = 0)]
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