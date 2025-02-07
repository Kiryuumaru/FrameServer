using Application.Configuration.Extensions;
using ApplicationBuilderHelpers;
using CliFx.Attributes;

namespace Presentation.Commands;

[Command(Description = "The main client command.")]
public class MainCommand : BaseCommand
{
    [CommandOption("api-urls", Description = "API port for configurations.", EnvironmentVariable = ApplicationConfigurationExtensions.ApiUrlsKey)]
    public string ApiUrls { get; set; } = "http://*:22200";

    [CommandOption("config-file-filter", Description = "The filename filter for the config.", EnvironmentVariable = ApplicationConfigurationExtensions.ConfigFileFilterKey)]
    public string ConfigFileFilter { get; set; } = "frame_server_config*.yml";

    public override async ValueTask Run(ApplicationHostBuilder<WebApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        appBuilder.Configuration.SetApiUrls(ApiUrls);
        appBuilder.Configuration.SetConfigFileFilter(ConfigFileFilter);

        await appBuilder.Build().Run(stoppingToken);
    }
}
