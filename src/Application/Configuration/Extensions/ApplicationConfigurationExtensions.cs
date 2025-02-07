using Microsoft.Extensions.Configuration;

namespace Application.Configuration.Extensions;

public static class ApplicationConfigurationExtensions
{
    public const string ApiUrlsKey = $"{ApplicationDefaults.EnvironmentVarsPreName}_API_URLS";
    public static string GetApiUrls(this IConfiguration configuration)
    {
        return configuration.GetVarRefValue(ApiUrlsKey);
    }
    public static void SetApiUrls(this IConfiguration configuration, string apiUrls)
    {
        configuration[ApiUrlsKey] = apiUrls;
    }

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
