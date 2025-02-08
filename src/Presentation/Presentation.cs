using Application;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.Options;
using System.Reflection;
using Application.Configuration.Extensions;
using Presentation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation;

internal class Presentation : Application.Application
{
    public override void AddConfiguration(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfiguration(applicationBuilder, configuration);

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
    }
}
