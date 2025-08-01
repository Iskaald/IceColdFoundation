using UnityEngine;

namespace IceCold.Interface
{
    public class GoogleSheetsConfig : IceColdConfig
    {
        public static string ConfigKey => nameof(GoogleSheetsConfig);
        public override string Key => ConfigKey;

        [Tooltip("Your Google Sheets API key. See README for setup instructions.")]
        public string apiKey;
    }
}