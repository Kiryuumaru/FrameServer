#pragma once

#include <stack>
#include <mutex>
#include <shared_mutex>
#include <random>
#include <regex>
#include <string>
#include <fstream>
#include <filesystem>
#include <future>
#include <algorithm>
#include <iostream>
#include <cstdio>
#include <ctime>
#include <sstream>
#include <iomanip>
#include <chrono>
#include <thread>
#include <condition_variable>
#include <map>
#include <cctype>
#include <stdexcept>
#include <regex>
#include <locale>
#include <cmath>
#include <memory>
#include <list>
#include <queue>
#include <set>
#include <sys/types.h>
#include <string_view>

#include <args.hxx>
#include <fmt/format.h>
#include <nlohmann/json.hpp>
#include <yaml-cpp/yaml.h>
#include <yaml-cpp/node/node.h>
#include <yaml-cpp/binary.h>
#include <spdlog/spdlog.h>
#include <spdlog/pattern_formatter.h>
#include <spdlog/sinks/daily_file_sink.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <httplib.h>
#include <websocketpp/server.hpp>
#include <websocketpp/client.hpp>
#include <websocketpp/config/asio.hpp>
#include <websocketpp/config/asio_client.hpp>
#include <websocketpp/config/asio_no_tls.hpp>
#include <websocketpp/config/asio_no_tls_client.hpp>
#include <openssl/evp.h>

//Platform specific
#if defined(WINDOWS)
#include <windows.h>
#elif defined(LINUX)
#include <unistd.h>
#include <dirent.h>
#include <sys/sysinfo.h>
#include <sys/statvfs.h>
#include <sys/utsname.h>
#endif

#define CPPHTTPLIB_ZLIB_SUPPORT

using nlohmann::json;
