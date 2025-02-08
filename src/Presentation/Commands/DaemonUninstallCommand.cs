using ApplicationBuilderHelpers;
using CliFx.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Services;

namespace Presentation.Commands;

[Command("daemon uninstall", Description = "Daemon uninstall command.")]
public class DaemonUninstallCommand : BaseCommand
{
    public override async ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        var appHost = appBuilder.Build();

        var clientServiceManager = appHost.Host.Services.GetRequiredService<ClientManager>();

        await clientServiceManager.Uninstall(stoppingToken);
    }
}
