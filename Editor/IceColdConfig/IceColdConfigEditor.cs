using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using IceCold.GoogleSheetsIntegration.Editor;
using IceCold.Interface;

namespace IceCold.Editor
{
    [CustomEditor(typeof(BaseConfigNode), true)]
    public class IceColdConfigEditor : UnityEditor.Editor
    {
        private List<string> sheetTabs = new();
        private bool isFetchingTabs;
        private string fetchTabsError = "";
        
        private SerializedProperty useGoogleSheetsProp;
        private SerializedProperty googleSheetUrlProp;
        private SerializedProperty selectedTabProp;

        private GoogleSheetsIntegrationUtility gSheetsUtil = new();
        
        private void OnEnable()
        {
            useGoogleSheetsProp = serializedObject.FindProperty("useGoogleSheets");
            googleSheetUrlProp = serializedObject.FindProperty("googleSheetUrl");
            selectedTabProp = serializedObject.FindProperty("selectedTab");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Data Source", EditorStyles.boldLabel);

            if (GUILayout.Button("Export to CSV File"))
            {
                gSheetsUtil.ExportConfigToCsv((BaseConfigNode)target);
            }
            
            EditorGUILayout.PropertyField(useGoogleSheetsProp, new GUIContent("Import from Google Sheets"));

            if (useGoogleSheetsProp.boolValue)
            {
                EditorGUILayout.PropertyField(googleSheetUrlProp, new GUIContent("Google Sheet URL"));
                EditorGUILayout.HelpBox("The sheet must be shared with 'Anyone with the link can view' for imports to work.", MessageType.Info);

                // Disable all Google Sheet controls if the URL is not set
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(googleSheetUrlProp.stringValue));
                
                var apiKey = gSheetsUtil.GetGoogleSheetsApiKey();

                if (string.IsNullOrEmpty(apiKey))
                {
                    // --- NO API KEY PATH ---
                    // The user must manually enter the tab name.
                    EditorGUILayout.HelpBox(
                        "Google Sheets API key not set. Tab list cannot be fetched automatically. " +
                        "Please enter the exact tab name manually below.",
                        MessageType.Warning
                    );
                    
                    EditorGUILayout.PropertyField(selectedTabProp, new GUIContent("Sheet Tab Name"));

                    // Enable the download button only if a tab name has been entered.
                    EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedTabProp.stringValue));
                    if (GUILayout.Button("Download & Import from Tab"))
                    {
                        gSheetsUtil.DownloadAndImportCsv((BaseConfigNode)target, googleSheetUrlProp.stringValue, selectedTabProp.stringValue);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    // --- API KEY EXISTS PATH ---
                    if (GUILayout.Button("Refresh tabs"))
                    {
                        FetchTabs();
                    }

                    if (isFetchingTabs)
                    {
                        EditorGUILayout.LabelField("Fetching tabs, please wait...");
                    }
                    else if (!string.IsNullOrEmpty(fetchTabsError))
                    {
                        EditorGUILayout.HelpBox($"Error fetching tabs: {fetchTabsError}", MessageType.Error);
                    }
                    else if (sheetTabs.Count > 0)
                    {
                        EditorGUILayout.Space(5);
                        
                        var selectedIndex = sheetTabs.IndexOf(selectedTabProp.stringValue);
                        if (selectedIndex < 0) selectedIndex = 0;

                        selectedIndex = EditorGUILayout.Popup("Sheet Tab", selectedIndex, sheetTabs.ToArray(), EditorStyles.toolbarPopup);
                        selectedTabProp.stringValue = sheetTabs[selectedIndex];
                        
                        EditorGUILayout.Space(5);

                        if (GUILayout.Button("Download & Import from Selected Tab"))
                        {
                            gSheetsUtil.DownloadAndImportCsv((BaseConfigNode)target, googleSheetUrlProp.stringValue, selectedTabProp.stringValue);
                        }
                    }
                }
                
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Config Data", EditorStyles.boldLabel);
            DrawPropertiesExcluding(serializedObject, "m_Script", "useGoogleSheets", "googleSheetUrl", "selectedTab");

            serializedObject.ApplyModifiedProperties();
        }

        private void FetchTabs()
        {
            isFetchingTabs = true;
            fetchTabsError = "";
            sheetTabs.Clear();
            Repaint();
            
            gSheetsUtil.FetchTabs(googleSheetUrlProp.stringValue, OnTabsFetched);
        }
        
        private void OnTabsFetched(string error, SpreadsheetResponse spreadsheetData)
        {
            sheetTabs.Clear();
            fetchTabsError = error;
            
            if (spreadsheetData != null && spreadsheetData.sheets != null)
            {
                foreach (var sheet in spreadsheetData.sheets)
                {
                    sheetTabs.Add(sheet.properties.title);
                }
            }
            
            isFetchingTabs = false;
            Repaint();
        }
    }
}