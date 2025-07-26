using IceCold.Interface;

namespace IceCold
{
    public static class ConfigLoader
    {
        public static T GetConfig<T>(string key) where T : CoreConfig
        {
            return CoreConfig.GetConfig<T>(key);
        }
    }
}