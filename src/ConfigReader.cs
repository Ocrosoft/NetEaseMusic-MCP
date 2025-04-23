using Microsoft.Extensions.Configuration;

namespace NetEaseMusic_MCP
{
    internal class ConfigReader
    {
        private static IConfiguration? _configuration = null;
        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration != null)
                {
                    return _configuration;
                }
                ConfigurationBuilder builder = new();
                builder.SetBasePath(Directory.GetCurrentDirectory());
                builder.AddJsonFile("appsettings.json");
                _configuration = builder.Build();
                return _configuration;
            }
        }

        public static T? GetConfig<T>(string key)
        {
            var config = Configuration[key];
            if (config == null)
            {
                return default;
            }
            return (T)Convert.ChangeType(config, typeof(T));
        }
    }
}
