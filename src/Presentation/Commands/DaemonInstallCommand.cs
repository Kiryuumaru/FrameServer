using ApplicationBuilderHelpers;
using CliFx.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Services;

namespace Presentation.Commands;

[Command("daemon install", Description = "Daemon install command.")]
public class DaemonInstallCommand : BaseCommand
{
    [CommandOption("server", Description = "Install server daemon.")]
    public bool Server { get; set; }

    [CommandOption("username", 'u', Description = "Username of the service account.")]
    public string? Username { get; set; }

    [CommandOption("password", 'p', Description = "Password of the service account.")]
    public string? Password { get; set; }

    public override async ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        var appHost = appBuilder.Build();

        var clientServiceManager = appHost.Host.Services.GetRequiredService<ClientManager>();

        await clientServiceManager.Install(Username, Password, stoppingToken);
    }
}
