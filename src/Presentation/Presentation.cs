using Application;
using ApplicationBuilderHelpers;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using System.Reflection;
using Application.Configuration.Extensions;
using Presentation.Services;

namespace Presentation;

internal class Presentation : Application.Application
{
    public override void AddConfiguration(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfiguration(applicationBuilder, configuration);

        (applicationBuilder.Builder as WebApplicationBuilder)!.WebHost.UseUrls(configuration.GetApiUrls());

        (configuration as ConfigurationManager)!.AddEnvironmentVariables();
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddScoped<ClientManager>();

        services.AddHttpClient(Options.DefaultName, client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", ApplicationDefaults.ApplicationUserAgentName);
        });

        services.AddMvc();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        base.AddMiddlewares(applicationHost, host);
    }

    public override void AddMappings(ApplicationHost applicationHost, IHost host)
    {
        base.AddMappings(applicationHost, host);

        (host as WebApplication)!.UseAuthorization();
        (host as WebApplication)!.MapControllers();

        (host as WebApplication)!.UseAntiforgery();
    }
}
