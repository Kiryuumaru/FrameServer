#pragma once

#include "viana/application/logger.h"
#include "viana/application/commands/command.h"
#include "viana/application/common_include.h"
#include "viana/domain/args.h"

#include "domain/version_args.h"

struct VersionCommand : Command<VersionArgs> {
	inline VersionCommand(std::shared_ptr<Logger> parentLogger = nullptr) : Command<VersionArgs>("VERSION", parentLogger) { }

	int runInternal() override;
};
