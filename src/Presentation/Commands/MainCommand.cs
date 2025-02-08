using Application.Configuration.Extensions;
using ApplicationBuilderHelpers;
using CliFx.Attributes;
using Microsoft.Extensions.Hosting;

namespace Presentation.Commands;

[Command(Description = "The main client command.")]
public class MainCommand : BaseCommand
{
    [CommandOption("config-file-filter", Description = "The filename filter for the config.", EnvironmentVariable = ApplicationConfigurationExtensions.ConfigFileFilterKey)]
    public string ConfigFileFilter { get; set; } = "frame_server_config*.yml";

    public override async ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        appBuilder.Configuration.SetConfigFileFilter(ConfigFileFilter);

        await appBuilder.Build().Run(stoppingToken);
    }
}
