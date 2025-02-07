using Application.Configuration.Services;
using Application.Configuration.Workers;
using Application.ServiceMaster.Services;
using ApplicationBuilderHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public class Application : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddScoped<ServiceManagerService>();
        services.AddScoped<DaemonManagerService>();

        services.AddSingleton<FrameSourceConfigurationService>();
        services.AddHostedService<FrameSourceConfigurationWorkers>();
    }
}
