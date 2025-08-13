using UnityEngine;

namespace IceCold.Interface
{
    public class GoogleSheetsConfig : IceColdConfig
    {
        [Tooltip("Your Google Sheets API key. See README for setup instructions.")]
        public string apiKey;
    }
}