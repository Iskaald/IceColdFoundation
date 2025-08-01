using IceCold.Interface;
using UnityEditor;

namespace IceCold.Editor
{
    public class GoogleSheetsIntegrationMenu : IceColdMenu
    {
        [MenuItem("IceCold/Google Sheets Integration/Config", priority = 3)]
        private static void SelectGoogleSheetsConfig()
        {
            var config = FindConfigAsset<GoogleSheetsConfig>();
            
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }
    }
}