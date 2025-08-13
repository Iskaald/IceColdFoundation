using UnityEngine;

namespace IceCold.Interface
{
    public class GoogleSheetsConfig : IceColdConfig
    {
        public override string Key => nameof(GoogleSheetsConfig);
        
        [Tooltip("Your Google Sheets API key. See README for setup instructions.")]
        public string apiKey;
    }
}