using AbsolutePathHelpers;
using Application;
using Application.Configuration.Extensions;
using ApplicationBuilderHelpers;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using Infrastructure.Serilog;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Presentation.Commands;

public abstract class BaseCommand : ICommand
{
    [CommandOption("log-level", 'l', Description = "Level of logs to show.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    [CommandOption("as-json", Description = "Output as json.")]
    public bool AsJson { get; set; } = false;

    [CommandOption("home", Description = "Home directory.", EnvironmentVariable = CommonConfigurationExtensions.HomePathKey)]
    //public string Home { get; set; } = AbsolutePath.Start(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) / ApplicationDefaults.ExecutableName;
    public string Home { get; set; } = AbsolutePath.Create(Environment.CurrentDirectory) / ApplicationDefaults.ExecutableName;

    public ApplicationHostBuilder<HostApplicationBuilder> CreateBuilder()
    {
        var appBuilder = ApplicationHost.FromBuilder(Host.CreateApplicationBuilder())
            .Add<Presentation>()
            .Add<SerilogInfrastructure>();

        appBuilder.Configuration.SetLoggerLevel(LogLevel);

        try
        {
            appBuilder.Configuration.SetHomePath(AbsolutePath.Create(Home));
        }
        catch
        {
            throw new CommandException($"Invalid home directory \"{Home}\".", 1000);
        }

        return appBuilder;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var appBuilder = CreateBuilder();
        var cancellationToken = console.RegisterCancellationHandler();

        try
        {
            await Run(appBuilder, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    public abstract ValueTask Run(ApplicationHostBuilder<HostApplicationBuilder> appBuilder, CancellationToken stoppingToken);
}
