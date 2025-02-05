#pragma once

#include "viana/application/common_include.h"

struct ThreadCompatRef {
	std::thread coreThread;

	bool isStarted = false;

	bool isCompleted = false;

	void wait() {
		if (coreThread.joinable()) {
			coreThread.join();
		}
	}

	void detach() {
		coreThread.detach();
	}
};

struct ThreadCompat {

private:
	std::shared_ptr<ThreadCompatRef> tc = std::make_shared<ThreadCompatRef>();

public:
	ThreadCompat() { }

	ThreadCompat(std::function<void()> func) {
		tc->coreThread = std::thread([tcCopy = tc, func]() {
			tcCopy->isStarted = true;
			func();
			tcCopy->isCompleted = true;
			});
	}

	ThreadCompat(std::vector<ThreadCompat> tcs) {
		tc->coreThread = std::thread([tcCopy = tc, tcs]() {
			tcCopy->isStarted = true;
			for (auto& t : tcs) {
				t->wait();
			}
			tcCopy->isCompleted = true;
			});
	}

	ThreadCompatRef* operator->() const noexcept {
		return tc.get();
	}
};
