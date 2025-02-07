using AbsolutePathHelpers;
using Application;
using Application.Configuration.Extensions;
using System.Runtime.InteropServices;
using Application.ServiceMaster.Services;
using Application.Common.Extensions;

namespace Presentation.Services;

internal class ClientManager(ILogger<ClientManager> logger, IConfiguration configuration, ServiceManagerService serviceManager, DaemonManagerService daemonManager)
{
    private readonly ILogger<ClientManager> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly ServiceManagerService _serviceManager = serviceManager;
    private readonly DaemonManagerService _daemonManager = daemonManager;

    public async Task UpdateClient(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(ClientManager), nameof(UpdateClient));

        _logger.LogInformation("Downloading latest client...");

        string folderName;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            folderName = $"{ApplicationDefaults.AppNamePascalCase}_WindowsX64";
        }
        else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            folderName = $"{ApplicationDefaults.AppNamePascalCase}_WindowsARM64";
        }
        else
        {
            throw new NotSupportedException();
        }

        await _serviceManager.Download(
            ApplicationDefaults.AppNameKebabCase,
            $"${ApplicationDefaults.GithubRepoUrl}/releases/latest/download/{folderName}.zip",
            "latest",
            async extractFactory =>
            {
                var extractTemp = _configuration.GetTempPath() / $"{ApplicationDefaults.ExecutableName}-{Guid.NewGuid()}";
                await extractFactory.DownloadedFilePath.UnZipTo(extractTemp, cancellationToken);
                await (extractTemp / folderName / $"{ApplicationDefaults.ExecutableName}.exe").CopyTo(extractFactory.ExtractDirectory / $"{ApplicationDefaults.ExecutableName}.exe");
            },
            executableLinkFactory => [(executableLinkFactory / $"{ApplicationDefaults.ExecutableName}.exe", $"{ApplicationDefaults.ExecutableName}.exe")],
            cancellationToken);

        _logger.LogInformation("Latest client downloaded");
    }

    public async Task Install(string? username, string? password, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(ClientManager), nameof(Install));

        _logger.LogInformation("Installing client...");

        var appServicePath = await _serviceManager.GetCurrentServicePath(ApplicationDefaults.AppNameKebabCase, cancellationToken)
            ?? throw new Exception("app client was not downloaded");
        var appExecPath = appServicePath / $"{ApplicationDefaults.ExecutableName}.exe";

        await _daemonManager.Install(
            ApplicationDefaults.AppNameKebabCase,
            ApplicationDefaults.AppNameReadable,
            ApplicationDefaults.AppNameDescription,
            appExecPath,
            "",
            username,
            password,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = "http://*:23456",
                [CommonConfigurationExtensions.MakeFileLogsKey] = "yes"
            },
            cancellationToken);

        _logger.LogInformation("Service client installed");
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(ClientManager), nameof(Start));

        _logger.LogInformation("Starting client service...");

        await _daemonManager.Start(ApplicationDefaults.AppNameKebabCase, cancellationToken);

        _logger.LogInformation("Service client started");
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(ClientManager), nameof(Stop));

        _logger.LogInformation("Stopping client service...");

        await _daemonManager.Stop(ApplicationDefaults.AppNameKebabCase, cancellationToken);

        _logger.LogInformation("Service client stopped");
    }

    public async Task Uninstall(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(ClientManager), nameof(Uninstall));

        _logger.LogInformation("Uninstalling client service...");

        await _daemonManager.Uninstall(ApplicationDefaults.AppNameKebabCase, cancellationToken);

        _logger.LogInformation("Service client uninstalled");
    }
}
