#pragma once

#include "domain/args_value.h"

struct Args {
	ArgsValue<bool> asJson;

	ArgsValue<std::string> logLevel;

	void patch(Args args) {
		asJson.patch(args.asJson);
		logLevel.patch(args.logLevel);
	}
};
