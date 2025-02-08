using Application.Logger.Interfaces;
using ApplicationBuilderHelpers;
using Infrastructure.Serilog.Common;
using Infrastructure.Serilog.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;

namespace Infrastructure.Serilog;

public class SerilogInfrastructure : ApplicationDependency
{
    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddTransient<ILoggerReader, SerilogLoggerReader>();

        services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddSerilog(LoggerBuilder.Configure(new LoggerConfiguration(), applicationBuilder.Configuration).CreateLogger());
        });

        Log.Logger = LoggerBuilder.Configure(new LoggerConfiguration(), applicationBuilder.Configuration).CreateLogger();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        base.AddMiddlewares(applicationHost, host);
    }
}
