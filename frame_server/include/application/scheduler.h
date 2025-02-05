#pragma once

#include "viana/application/common_include.h"
#include "viana/application/common.h"
#include "viana/application/thread_compat.h"
#include "viana/application/cancellation_token.h"

struct Scheduler {
	static ThreadCompat createMillis(int milis, bool runFirst, std::function<void()> func, CancellationToken ct) {
		return ThreadCompat([&, milis, runFirst, func, ct]() {
			uint64_t epoch = 0;
			if (!runFirst) {
				epoch = Common::getEpochTimeMs();
			}
			while (!ct->isCanceled()) {
				if (Common::getEpochTimeMs() >= epoch + milis) {
					epoch = Common::getEpochTimeMs();
					func();
				}
				std::this_thread::sleep_for(std::chrono::milliseconds(10));
			}
			});
	}

	static ThreadCompat createMillis(int milis, std::function<void()> func, CancellationToken ct) {
		return createMillis(milis, true, func, ct);
	}

	static ThreadCompat createSeconds(int seconds, bool runFirst, std::function<void()> func, CancellationToken ct) {
		return createMillis(seconds * 1000, runFirst, func, ct);
	}

	static ThreadCompat createSeconds(int seconds, std::function<void()> func, CancellationToken ct) {
		return createMillis(seconds * 1000, true, func, ct);
	}
};