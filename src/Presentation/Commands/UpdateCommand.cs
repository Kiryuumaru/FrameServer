using ApplicationBuilderHelpers;
using CliFx.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Services;

namespace Presentation.Commands;

[Command("update", Description = "Update client.")]
public class UpdateCommand : BaseCommand
{
    public override async ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken)
    {
        var appHost = appBuilder.Build();

        var serviceManager = appHost.Host.Services.GetRequiredService<ClientManager>();

        await serviceManager.UpdateClient(stoppingToken);
    }
}
