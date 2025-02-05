#pragma once

#include "domain/args.h"

struct VersionArgs : Args {
	ArgsValue<bool> all;

	ArgsValue<bool> id;
};
