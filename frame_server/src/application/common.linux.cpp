#if defined(LINUX)

#include "application/common.h"

bool Common::isElevated() {
    return getuid() == 0;
}

#endif
