using UnityEngine;

namespace IceCold.Interface
{
    /// <summary>
    /// The non-generic base class for all configuration assets.
    /// This holds the editor-facing logic for data sources like Google Sheets.
    /// </summary>
    public abstract class BaseConfigNode : IceColdConfig
    {
        [Tooltip("A unique key used to identify and load this configuration asset.")]
        [SerializeField]
        protected string configKey;

        [Header("Data Source Settings")]
        [Tooltip("If checked, allows importing data from a public Google Sheet.")]
        public bool useGoogleSheets = false;
        public string googleSheetUrl = "";
        [HideInInspector]
        public string selectedTab = "";

        /// <summary>
        /// Parses a raw CSV string and populates the internal config data object.
        /// </summary>
        /// <returns>True if the data was successfully parsed and applied.</returns>
        public abstract bool ParseCsv(string csv);

        /// <summary>
        /// Exports the internal config data object to a CSV string.
        /// </summary>
        public abstract string ExportToCsv();

        /// <summary>
        /// A non-generic way for the runtime loader to get the typed config data object.
        /// </summary>
        public abstract object GetConfigData();
    }
}