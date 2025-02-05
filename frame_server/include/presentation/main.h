#pragma once

#include "application/common.h"
#include "application/logger.h"
#include "viana/application/argsparser.h"
#include "viana/application/common_include.h"
#include "viana/domain/json_result.h"

#include "application/commands/main_command.h"
#include "application/commands/version_command.h"
#include "domain/version_args.h"

struct ConsoleApp {
	inline static std::shared_ptr<Logger> log = Logger::generate("CONSOLE");

	int run(int argc, char* argv[]);
};
