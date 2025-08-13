using UnityEngine;

namespace IceCold.Interface
{
    [CreateAssetMenu(fileName = nameof(LoggerConfig), menuName = "IceCold/Logger/Create Config", order = 0)]
    public class LoggerConfig : IceColdConfig
    {
        public override string Key => nameof(LoggerConfig);
        
        public LogFilterSettings debugFilterSettings = new(true, true, true);
        public LogFilterSettings releaseFilterSettings = new(false, false, true);
        public LogFilterSettings editorFilterSettings = new(true, true, true);
    }
}