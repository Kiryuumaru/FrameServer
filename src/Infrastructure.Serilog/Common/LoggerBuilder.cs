using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Application.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Infrastructure.Serilog.Enrichers;
using Microsoft.Extensions.Logging;
using Application;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Infrastructure.Serilog.Common;

internal static class LoggerBuilder
{
    public static LoggerConfiguration Configure(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        LogLevel logLevel = configuration.GetLoggerLevel();
        LogEventLevel logEventLevel = logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new NotSupportedException(logLevel.ToString())
        };

        loggerConfiguration = loggerConfiguration
            .MinimumLevel.Is(logEventLevel)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.With(new LogGuidEnricher(configuration))
            .WriteTo.Console(new ExpressionTemplate(
                template: "[{@t:yyyy-MM-dd HH:mm:ss} {@l:u3}]{#if Service is not null} ({Service}){#end} {@m} \n{@x}", theme: TemplateTheme.Code),
                restrictedToMinimumLevel: logEventLevel);

        string? useOtlpExporterEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(useOtlpExporterEndpoint))
        {
            loggerConfiguration = loggerConfiguration
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = useOtlpExporterEndpoint;
                    options.ResourceAttributes.Add("service.name", ApplicationDefaults.LogServiceName);
                });
        }

        if (configuration.GetMakeFileLogs())
        {
            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Verbose()
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: configuration.GetDataPath() / "logs" / "log-.jsonl",
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    rollingInterval: RollingInterval.Hour);
        }

        return loggerConfiguration;
    }
}
