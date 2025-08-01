using System.Collections.Generic;

namespace IceCold.GoogleSheetsIntegration.Editor
{
    [System.Serializable]
    public class SpreadsheetResponse
    {
        public List<Sheet> sheets;
    }

    [System.Serializable]
    public class Sheet
    {
        public SheetProperties properties;
    }

    [System.Serializable]
    public class SheetProperties
    {
        public string title;
    }
}