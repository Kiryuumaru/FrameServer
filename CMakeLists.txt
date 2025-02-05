cmake_minimum_required(VERSION 3.21)

project(frame_server)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_INSTALL_PREFIX install)
set(BUILD_SHARED_LIBS OFF)
set(COMPILE_WARNING_AS_ERROR OFF)
set(CMAKE_COMPILE_WARNING_AS_ERROR OFF)

string(TOUPPER ${CMAKE_TARGET_OS} CMAKE_TARGET_OS_UPPER)
string(TOUPPER ${CMAKE_TARGET_ARCH} CMAKE_TARGET_ARCH_UPPER)
string(TOLOWER ${CMAKE_TARGET_OS} CMAKE_TARGET_OS_LOWER)
string(TOLOWER ${CMAKE_TARGET_ARCH} CMAKE_TARGET_ARCH_LOWER)

if(CMAKE_TARGET_OS_LOWER STREQUAL "windows")
    # cmake_policy(SET CMP0091 NEW)
    # set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /verbose:lib")
    # add_link_options("/NODEFAULTLIB:libc.lib /NODEFAULTLIB:msvcrt.lib /NODEFAULTLIB:libcd.lib /NODEFAULTLIB:msvcrtd.lib")
    # set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    add_compile_definitions(_SILENCE_ALL_MS_EXT_DEPRECATION_WARNINGS)
elseif(CMAKE_TARGET_OS_LOWER STREQUAL "linux")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -pthread -lpthread")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -pthread -lpthread")
else()
    message(FATAL_ERROR "Unsupported OS ${CMAKE_TARGET_OS}")
endif()

set(BUILD_OS "${CMAKE_TARGET_OS_LOWER}")
set(BUILD_ARCH "${CMAKE_TARGET_ARCH_LOWER}")
set(BUILD_RUNTIME "${BUILD_OS}-${BUILD_ARCH}")

add_compile_definitions("${CMAKE_TARGET_OS_UPPER}")
add_compile_definitions(BUILD_OS="${BUILD_OS}")
add_compile_definitions(BUILD_ARCH="${BUILD_ARCH}")
add_compile_definitions(BUILD_RUNTIME="${BUILD_OS}-${BUILD_ARCH}")

message(STATUS "BUILD_RUNTIME: ${BUILD_RUNTIME}")

# Resolve config build type
string(TOUPPER ${CMAKE_CURRENT_BINARY_DIR} CMAKE_CURRENT_BINARY_DIR_UPPER)
if (CMAKE_CURRENT_BINARY_DIR_UPPER MATCHES "DEBUG")
    set(BUILD_TYPE "Debug")
else()
    set(BUILD_TYPE "Release")
endif()

# Project setup
function(setup project_display_name)
    add_compile_definitions(PROJECT_DISPLAY_NAME="${project_display_name}")
endfunction()

# Resolve version
# set(BIN_RUNTIME "pr.242")
set(BIN_RUNTIME "$ENV{BIN_RUNTIME}")
set(BUILD_ID "$ENV{BUILD_ID}")
set(PROJECT_VERSION "$ENV{PROJECT_VERSION}")
if ((NOT BUILD_ID) OR BUILD_ID STREQUAL "")
    set(BUILD_ID "dev")
endif()
if ((NOT PROJECT_VERSION) OR (PROJECT_VERSION STREQUAL ""))
    set(PROJECT_VERSION "0.0.0-preview.0")
endif()
string(TOLOWER "${PROJECT_VERSION}" PROJECT_VERSION)
string(REPLACE "-" ";" PROJECT_VERSION_SEP ${PROJECT_VERSION})
list(POP_FRONT PROJECT_VERSION_SEP PROJECT_VERSION_CORE)
if(PROJECT_VERSION_SEP STREQUAL "")
    set(PROJECT_VERSION_PRE_RELEASE "")
    set(PROJECT_VERSION_PRE_RELEASE_TAG "")
    set(PROJECT_VERSION_PRE_RELEASE_NUMBER "")
    set(PROJECT_VERSION_RELEASE_TYPE "main")
else()
    list(POP_FRONT PROJECT_VERSION_SEP PROJECT_VERSION_PRE_RELEASE)
    set(PROJECT_VERSION_PRE_RELEASE "${PROJECT_VERSION_PRE_RELEASE}")
    string(REPLACE "." ";" PROJECT_VERSION_PRE_RELEASE_SEP ${PROJECT_VERSION_PRE_RELEASE})
    list(POP_FRONT PROJECT_VERSION_PRE_RELEASE_SEP PROJECT_VERSION_PRE_RELEASE_TAG)
    list(POP_FRONT PROJECT_VERSION_PRE_RELEASE_SEP PROJECT_VERSION_PRE_RELEASE_NUMBER)
    set(PROJECT_VERSION_RELEASE_TYPE ${PROJECT_VERSION_PRE_RELEASE_TAG})
endif()
if ((NOT BIN_RUNTIME) OR (BIN_RUNTIME STREQUAL ""))
    set(BIN_RUNTIME ${PROJECT_VERSION_RELEASE_TYPE})
endif()
if (BIN_RUNTIME MATCHES "^pr*" OR (NOT BIN_RUNTIME MATCHES ""))
    set(PROJECT_FULL_VERSION "${PROJECT_VERSION}+build.${BUILD_ID}-${BIN_RUNTIME}")
else()
    set(PROJECT_FULL_VERSION "${PROJECT_VERSION}+build.${BUILD_ID}")
endif()
add_compile_definitions(PROJECT_VERSION="${PROJECT_VERSION}")
add_compile_definitions(PROJECT_VERSION_CORE="${PROJECT_VERSION_CORE}")
add_compile_definitions(PROJECT_FULL_VERSION="${PROJECT_FULL_VERSION}")
add_compile_definitions(PROJECT_VERSION_PRE_RELEASE="${PROJECT_VERSION_PRE_RELEASE}")
add_compile_definitions(PROJECT_VERSION_PRE_RELEASE_TAG="${PROJECT_VERSION_PRE_RELEASE_TAG}")
add_compile_definitions(PROJECT_VERSION_PRE_RELEASE_NUMBER="${PROJECT_VERSION_PRE_RELEASE_NUMBER}")
add_compile_definitions(PROJECT_VERSION_RELEASE_TYPE="${PROJECT_VERSION_RELEASE_TYPE}")
add_compile_definitions(BUILD_ID="${BUILD_ID}")
add_compile_definitions(BIN_RUNTIME="${BIN_RUNTIME}")

# Common configs
set(skip_samples ON CACHE BOOL "Set slip_samples on" FORCE)
set(BUILD_TESTING OFF CACHE BOOL "Set BUILD_TESTING off" FORCE)
add_definitions(-DPAHO_WITH_SSL)
add_compile_definitions(CPPHTTPLIB_OPENSSL_SUPPORT)

find_package(args CONFIG REQUIRED)
find_package(fmt CONFIG REQUIRED)
find_package(nlohmann_json CONFIG REQUIRED)
find_package(yaml-cpp CONFIG REQUIRED)
find_package(spdlog CONFIG REQUIRED)
find_package(OpenSSL REQUIRED)
find_package(httplib CONFIG REQUIRED)
find_package(websocketpp CONFIG REQUIRED)
find_package(Boost REQUIRED)

list(APPEND COMMON_LINK_LIBS
    taywee::args
    fmt::fmt-header-only
    nlohmann_json
    yaml-cpp
    OpenSSL::SSL
    OpenSSL::Crypto
    spdlog::spdlog
    spdlog::spdlog_header_only
    httplib::httplib
    websocketpp::websocketpp
    Boost::boost
    uuid
    ${CMAKE_THREAD_LIBS_INIT}
    )
list(APPEND COMMON_LINK_DIRS
    )
list(APPEND COMMON_INCLUDE_DIRS
    )

# Include sub-projects.
add_subdirectory(frame_server)
