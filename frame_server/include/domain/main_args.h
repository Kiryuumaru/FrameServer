#pragma once

#include "domain/args.h"

struct MainArgs : Args {

	ArgsValue<bool> noWaitInit;

};
