#include "application/commands/version_command.h"

int VersionCommand::runInternal() {

    std::string frameServerVersion = std::string(PROJECT_FULL_VERSION);

    if (args.all.value)
    {
        std::string msg = "Frame Server Version: " + frameServerVersion;
        json::array_t frameServerAllVersion = {
            {
                {"name", "frame_server"},
                {"version", frameServerVersion},
                {"id", BUILD_ID},
                {"release", BIN_RUNTIME}
            }
        };

        log->info("ALL_EXECUTABLE_VERSION", msg, { {"frame_server", frameServerAllVersion} });
    }
    else if (args.id.value)
    {
        log->info("EXECUTABLE_BUILD_ID", "Frame Server Build ID: " + std::string(BUILD_ID), {
                    {"frame_server", BUILD_ID}
            });
    }
    else
    {
        log->info("EXECUTABLE_VERSION", "Frame Server Version: " + frameServerVersion, {
                    {"frame_server", frameServerVersion}
            });
    }

	return 0;
}