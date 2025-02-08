using ApplicationBuilderHelpers;
using CliFx.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Services;

namespace Presentation.Commands;

[Command("daemon stop", Description = "Daemon stop command.")]
public class DaemonStopCommand : BaseCommand
{
    public override async ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        var appHost = appBuilder.Build();

        var clientServiceManager = appHost.Host.Services.GetRequiredService<ClientManager>();

        await clientServiceManager.Stop(stoppingToken);
    }
}
