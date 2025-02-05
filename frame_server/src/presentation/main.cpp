#include "application/commands/main_command.h"
#include "application/commands/version_command.h"
#include "domain/version_args.h"

int main(int argc, char *argv[]) {

#if defined(WINDOWS)
    SetConsoleOutputCP(CP_UTF8);
#endif

    ArgsParser parser("vianad");
    parser.setHelpWidth(100);

    MainArgs args{};
    parser.setAction([&]() { return MainCommand().run(args); });
    parser.addFlag(args.asJson, { "as-json" }, "Output as json");
    parser.addFlag(args.noWaitInit, { "no-wait-init" }, "Do not wait for the init indicator");
    parser.addValueFlag<std::string>(args.logLevel, { 'l', "log-level" }, "Logging level", Logger::INFO_L, Logger::LoggingLevels);

    VersionArgs versionArgs{};
    std::shared_ptr<ArgsParserCommand> versionParser = parser.addCommand("version", "Version subcommand", [&]() { return VersionCommand().run(args, versionArgs); });
    versionParser->addFlag(versionArgs.all, { 'a', "all" }, "Get all the version of installed viana apps");
    versionParser->addFlag(versionArgs.id, { "id" }, "Get the version ID of the executable");

    return parser.parse(argc, argv);
}
