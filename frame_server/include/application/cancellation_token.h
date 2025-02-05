#pragma once

#include "application/common_include.h"
#include "application/common.h"

struct CancellationTokenRef {
private:
    std::vector<std::shared_ptr<CancellationTokenRef>> attached;

    bool canceled = false;

public:
    void link(std::vector<std::shared_ptr<CancellationTokenRef>> cts) {
        for (auto& c : cts) {
            attached.emplace_back(c);
        }
    }

    void cancel() {
        canceled = true;
    }

    bool isCanceled() {
        if (canceled) {
            return true;
        }

        for (const auto& token : attached) {
            if (token->isCanceled()) {
                return true;
            }
        }

        return false;
    }
};

struct CancellationToken {
private:
    std::shared_ptr<CancellationTokenRef> ct = std::make_shared<CancellationTokenRef>();

public:
    CancellationToken() { }

    CancellationToken(std::vector<CancellationToken> cts) {
        for (auto& c : cts) {
            ct->link({ c.ct });
        }
    }

    CancellationTokenRef* operator->() const noexcept {
        return ct.get();
    }
};
