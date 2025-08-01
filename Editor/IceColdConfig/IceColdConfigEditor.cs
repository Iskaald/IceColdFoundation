using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using IceCold.GoogleSheetsIntegration.Editor;
using IceCold.Interface;
using UnityEngine.Networking;

namespace IceCold.Editor
{
    [CustomEditor(typeof(IceColdConfig), true)]
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
            EditorGUILayout.LabelField("Google Sheets Integration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(useGoogleSheetsProp);

            if (useGoogleSheetsProp.boolValue)
            {
                var apiKey = gSheetsUtil.GetGoogleSheetsApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    EditorGUILayout.HelpBox(
                        "Google Sheets API key not set!\n\n" +
                        "To use Google Sheets integration, you must:\n" +
                        "1. Create a GoogleSheetsConfig asset (Assets > Create > IceCold > Google Sheets Config).\n" +
                        "2. Enter your API key (see README for instructions).",
                        MessageType.Warning
                    );
                }
                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(apiKey));

                EditorGUILayout.PropertyField(googleSheetUrlProp, new GUIContent("Google Sheet URL"));
                EditorGUILayout.HelpBox("You must enter your Google Sheets API key in the GoogleIntegrationConfig and the sheet must be shared with 'Anyone with the link can view' for this to work.", MessageType.Info);

                if (!string.IsNullOrEmpty(googleSheetUrlProp.stringValue))
                {
                    if (GUILayout.Button("Fetch Sheet Tabs"))
                    {
                        _ = FetchTabs(googleSheetUrlProp.stringValue);
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
                        var selectedIndex = sheetTabs.IndexOf(selectedTabProp.stringValue);
                        if (selectedIndex < 0) selectedIndex = 0;

                        selectedIndex = EditorGUILayout.Popup("Sheet Tab", selectedIndex, sheetTabs.ToArray());
                        selectedTabProp.stringValue = sheetTabs[selectedIndex];

                        if (GUILayout.Button("Download & Import from Selected Tab"))
                        {
                            _ = DownloadAndImportCsv((IceColdConfig)target, googleSheetUrlProp.stringValue, selectedTabProp.stringValue);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space(10);

                if (GUILayout.Button("Export to CSV File"))
                {
                    ExportConfigToCsv((IceColdConfig)target);
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
            
            DrawPropertiesExcluding(serializedObject, "m_Script", "useGoogleSheets", "googleSheetUrl", "selectedTab");

            serializedObject.ApplyModifiedProperties();
        }

        private async Task FetchTabs(string sheetUrl)
        {
            isFetchingTabs = true;
            fetchTabsError = "";
            sheetTabs.Clear();
            Repaint();

            try
            {
                var apiKey = GetGoogleSheetsApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    fetchTabsError = "Google Sheets API key not set! Please create a GoogleSheetsConfig asset and set your API key. See README for instructions.";
                    return;
                }

                var sheetId = ExtractSheetId(sheetUrl);
                var url = $"https://sheets.googleapis.com/v4/spreadsheets/{sheetId}?key={apiKey}&fields=sheets.properties.title";

                using var request = UnityWebRequest.Get(url);
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new System.Exception($"Network Error: {request.error}. Response Code: {request.responseCode}. Message: {request.downloadHandler.text}");
                }

                var responseJson = request.downloadHandler.text;
                var spreadsheetData = JsonUtility.FromJson<SpreadsheetResponse>(responseJson);
                
                if (spreadsheetData != null && spreadsheetData.sheets != null)
                {
                    sheetTabs.Clear();
                    foreach (var sheet in spreadsheetData.sheets)
                    {
                        sheetTabs.Add(sheet.properties.title);
                    }
                }

                if (sheetTabs.Count == 0)
                {
                    fetchTabsError = "Could not find any sheet tabs. Is the URL correct and the sheet public ('Anyone with the link can view')?";
                }
            }
            catch (System.Exception ex)
            {
                fetchTabsError = ex.Message;
                Debug.LogError($"[IceCold] Failed to fetch tabs: {ex}");
            }
            finally
            {
                isFetchingTabs = false;
                Repaint();
            }
        }

        private async Task DownloadAndImportCsv(IceColdConfig config, string sheetUrl, string tabName)
        {
            Debug.Log($"[IceCold] Starting download for tab '{tabName}'...");
            try
            {
                var apiKey = GetGoogleSheetsApiKey();
                var sheetId = ExtractSheetId(sheetUrl);
                var url = $"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv&sheet={UnityWebRequest.EscapeURL(tabName)}?key={apiKey}&fields=sheets.properties.title";
                
                using var request = UnityWebRequest.Get(url);
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new System.Exception(request.error);
                }
                
                string csv = request.downloadHandler.text;
                
                Undo.RecordObject(config, "Import from Google Sheets");
                ImportFromCsv(config, csv);
                EditorUtility.SetDirty(config);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IceCold] Failed to download or import CSV: {ex.Message}");
            }
        }
        
        private void ExportConfigToCsv(IceColdConfig config)
        {
            var csv = ExportToCsv(config);

            var path = EditorUtility.SaveFilePanel("Save Config as CSV", "Assets", config.Key + ".csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            
            File.WriteAllText(path, csv);
            Debug.Log($"[IceCold] Successfully exported config to {path}");
            EditorUtility.RevealInFinder(path);
        }

        private string ExtractSheetId(string url)
        {
            // Typical format: https://docs.google.com/spreadsheets/d/{SHEET_ID}/edit#gid=0
            var match = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
            if (!match.Success) throw new System.ArgumentException("Invalid Google Sheet URL. Could not find sheet ID.");
            return match.Groups[1].Value;
        }
        
        /// <summary>
        /// Imports data from a CSV string, populating the target config object's fields.
        /// </summary>
        private void ImportFromCsv(IceColdConfig targetConfig, string csv)
        {
            CsvUtility.FillObjectFromCsv(targetConfig, csv);
            Debug.Log($"Successfully imported data into {targetConfig.Key}.asset from CSV.");
        }

        /// <summary>
        /// Exports the serializable fields of a config object to a CSV string.
        /// </summary>
        private string ExportToCsv(IceColdConfig targetConfig)
        {
            return CsvUtility.ToCsv(targetConfig);
        }
        
        private string GetGoogleSheetsApiKey()
        {
            var config = Resources.Load<GoogleSheetsConfig>(GoogleSheetsConfig.ConfigKey);
            if (config != null && !string.IsNullOrEmpty(config.apiKey))
                return config.apiKey;

            return null;
        }
    }
}