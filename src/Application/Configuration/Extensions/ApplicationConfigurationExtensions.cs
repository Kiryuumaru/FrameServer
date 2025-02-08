using Microsoft.Extensions.Configuration;

namespace Application.Configuration.Extensions;

public static class ApplicationConfigurationExtensions
{
    public const string ConfigFileFilterKey = $"{ApplicationDefaults.EnvironmentVarsPreName}_CONFIG_FILE_FILTER";
    public static string GetConfigFileFilter(this IConfiguration configuration)
    {
        return configuration.GetVarRefValue(ConfigFileFilterKey);
    }
    public static void SetConfigFileFilter(this IConfiguration configuration, string configFileName)
    {
        configuration[ConfigFileFilterKey] = configFileName;
    }
}
