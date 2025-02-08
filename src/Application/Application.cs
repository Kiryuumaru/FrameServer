using Application.Configuration.Services;
using Application.Configuration.Workers;
using Application.FrameStreamer.Workers;
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

        services.AddSingleton<FrameSourceConfigurationHolderService>();
        services.AddScoped<FrameSourceConfigurationService>();
        services.AddHostedService<FrameSourceConfigurationWorker>();

        services.AddHostedService<FrameStreamerWorker>();
    }
}
