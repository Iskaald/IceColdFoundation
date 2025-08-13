using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IceCold.Interface;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace IceCold.GoogleSheetsIntegration.Editor
{
    public class GoogleSheetsIntegrationUtility
    {
        private GoogleSheetsConfig _config = null;
        private GoogleSheetsConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Resources.Load<GoogleSheetsConfig>(nameof(GoogleSheetsConfig));
                }
                return _config;
            }
        }
        
        public string GetGoogleSheetsApiKey()
        {
            if (Config != null && !string.IsNullOrEmpty(Config.apiKey))
                return Config.apiKey;

            return null;
        }

        public void FetchTabs(string sheetUrl, Action<string, SpreadsheetResponse> callback)
        {
            _ = FetchTabsAsync(sheetUrl, callback);
        }

        public void DownloadAndImportCsv(BaseConfigNode targetConfig, string sheetUrl, string tabName)
        {
            _ = DownloadAndImportCsvAsync(targetConfig, sheetUrl, tabName);
        }
        
        public void ExportConfigToCsv(BaseConfigNode targetConfig)
        {
            var csv = ExportToCsv(targetConfig);

            var path = EditorUtility.SaveFilePanel("Save Config as CSV", "Assets", targetConfig.Key + ".csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            
            File.WriteAllText(path, csv);
            IceColdLogger.Log($"[IceCold] Successfully exported config to {path}");
            EditorUtility.RevealInFinder(path);
        }
        
        private async Task FetchTabsAsync(string sheetUrl, Action<string,SpreadsheetResponse> callback)
        {
            var fetchTabsError = string.Empty;
            var spreadsheetData = new SpreadsheetResponse();

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
                    fetchTabsError =
                        $"Network Error: {request.error}. Response Code: {request.responseCode}. Message: {request.downloadHandler.text}";
                    
                    throw new Exception(fetchTabsError);
                }

                var responseJson = request.downloadHandler.text;
                spreadsheetData = JsonUtility.FromJson<SpreadsheetResponse>(responseJson);
                
                if (spreadsheetData == null || spreadsheetData.sheets == null || spreadsheetData.sheets.Count == 0)
                {
                    fetchTabsError = "Could not find any sheet tabs. Is the URL correct and the sheet public ('Anyone with the link can view')?";
                }
            }
            catch (Exception ex)
            {
                fetchTabsError = ex.Message;
            }
            finally
            {
                callback?.Invoke(fetchTabsError, spreadsheetData);
            }
        }
        
        private async Task DownloadAndImportCsvAsync(BaseConfigNode targetConfig, string sheetUrl, string tabName)
        {
            IceColdLogger.Log($"[IceCold] Starting download for tab '{tabName}'...");

            try
            {
                var sheetId = ExtractSheetId(sheetUrl);
                var url = $"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq?tqx=out:csv&sheet={UnityWebRequest.EscapeURL(tabName)}";
                
                using var request = UnityWebRequest.Get(url);
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Network Error: {request.error}. Response Code: {request.responseCode}. " +
                                        $"Is the sheet public ('Anyone with the link can view') and is the tab name '{tabName}' correct?");
                }

                var csv = request.downloadHandler.text;

                // Record the object's state before we attempt to change it.
                Undo.RecordObject(targetConfig, "Import from Google Sheets");
                var wasChanged = targetConfig.ParseCsv(csv);
                
                if (wasChanged)
                {
                    IceColdLogger.Log($"Successfully imported data into {targetConfig.name}.asset. Saving asset.");
                    EditorUtility.SetDirty(targetConfig);
                    AssetDatabase.SaveAssets(); // Or AssetDatabase.SaveAssetIfDirty(targetConfig);
                }
                else
                {
                    IceColdLogger.LogWarning($"Import for {targetConfig.name}.asset was skipped. The data may have been invalid or resulted in no changes.");
                    // We recorded an undo state, but nothing changed, so we clear it to avoid a "phantom" undo step.
                    Undo.ClearUndo(targetConfig);
                }
            }
            catch (Exception ex)
            {
                IceColdLogger.LogError($"[IceCold] Failed to download or import CSV: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Exports the serializable fields of a config object to a CSV string.
        /// </summary>
        private string ExportToCsv(BaseConfigNode targetConfig)
        {
            return targetConfig.ExportToCsv();
        }
        
        private string ExtractSheetId(string url)
        {
            // Typical format: https://docs.google.com/spreadsheets/d/{SHEET_ID}/edit#gid=0
            var match = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)");
            if (!match.Success) throw new ArgumentException("Invalid Google Sheet URL. Could not find sheet ID.");
            return match.Groups[1].Value;
        }
    }
}