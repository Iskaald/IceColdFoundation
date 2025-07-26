using System;

namespace IceCold.Interface
{
    [Serializable]
    public struct LogFilterSettings
    {
        public bool log;
        public bool warning;
        public bool error;

        public LogFilterSettings(bool log, bool warning, bool error)
        {
            this.log = log;
            this.warning = warning;
            this.error = error;
        }
    }
}