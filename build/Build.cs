using Nuke.Common.IO;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.Runner.Abstraction;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class Build : BaseNukeBuildHelpers
{
    public static int Main() => Execute<Build>(x => x.Interactive);

    public override string[] EnvironmentBranches { get; } = ["prerelease", "main"];

    public override string MainEnvironmentBranch => "main";

    private readonly string appId = "frame_server";
    private readonly OSPlatform[] runtimeMatrix = [OSPlatform.Windows, OSPlatform.Linux];
    private readonly Architecture[] archMatrix = [Architecture.X86, Architecture.Arm64];

    protected override WorkflowConfigEntry WorkflowConfig => _ => _
        .AppendReleaseNotesAssetHashes(false)
        .UseJsonFileVersioning(true);

    BuildEntry BuildEntry => _ => _
        .AppId(appId)
        .CheckoutFetchDepth(0)
        .CheckoutSubmodules(SubmoduleCheckoutType.Recursive)
        .CachePath(RootDirectory / "out")
        .CacheInvalidator("1")
        .Matrix(runtimeMatrix, (_, runtime) => _
            .Matrix(archMatrix, (_, arch) => _
                .WorkflowId($"build_{runtime.ToString().ToLowerInvariant()}_{arch.ToString().ToLowerInvariant()}")
                .DisplayName($"Build {runtime.ToString().ToLowerInvariant()}-{arch.ToString().ToLowerInvariant()}")
                .RunnerOS(() =>
                {
                    if (runtime == OSPlatform.Windows)
                        return RunnerOS.Windows2022;
                    else if (runtime == OSPlatform.Linux)
                        return RunnerOS.Ubuntu2204;
                    else
                        throw new NotSupportedException(runtime.ToString());
                })
                .Execute(context =>
                {

                })));

    public PublishEntry PublishAssets => _ => _
        .AppId(appId)
        .RunnerOS(RunnerOS.Ubuntu2204)
        .WorkflowId("publish")
        .DisplayName("Publish binaries")
        .ReleaseAsset(() =>
        {
            List<AbsolutePath> paths = [];
            foreach (var runtime in runtimeMatrix)
            {
                foreach (var arch in archMatrix)
                {
                    //paths.Add(GetReleaseArchivePath(runtime, arch));
                    //paths.Add(GetOneLineInstallScriptPath(runtime, arch));
                    Log.Information("Release archive: {runtime}, {arch}", runtime, arch);
                }
            }
            return [.. paths];
        });
}
