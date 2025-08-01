using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IceCold.Interface;
using UnityEngine;
using UnityEngine.Networking;

namespace IceCold.GoogleSheetsIntegration.Editor
{
    public class GoogleSheetsIntegrationUtility
    {
        private GoogleSheetsConfig config = null;
        
        public string GetGoogleSheetsApiKey()
        {
            if (config != null) return config.apiKey;
            
            config = Resources.Load<GoogleSheetsConfig>(GoogleSheetsConfig.ConfigKey);
            if (config != null && !string.IsNullOrEmpty(config.apiKey))
                return config.apiKey;

            return null;
        }
        
        public async Task FetchTabs(string sheetUrl, Action<string,List<string>> callback)
        {
            var fetchTabsError = string.Empty;
            var sheetTabs = new List<string>();

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
            catch (Exception ex)
            {
                fetchTabsError = ex.Message;
            }
            finally
            {
                callback?.Invoke(fetchTabsError, sheetTabs);
            }
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