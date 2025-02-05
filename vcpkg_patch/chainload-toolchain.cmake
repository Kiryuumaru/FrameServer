# Custom overlay toolchain

set(VCPKG_TARGET_TRIPLET "${CMAKE_TARGET_ARCH}-${CMAKE_TARGET_OS}")

if(CMAKE_TARGET_OS STREQUAL "linux")

    if(CMAKE_TARGET_ARCH STREQUAL "arm64")

        set(CMAKE_C_COMPILER "/usr/bin/aarch64-linux-gnu-gcc")
        set(CMAKE_CXX_COMPILER "/usr/bin/aarch64-linux-gnu-g++")

        set(CMAKE_PKGCONFIG_SYSROOT "/usr/lib/aarch64-linux-gnu/pkgconfig")

    elseif(CMAKE_TARGET_ARCH STREQUAL "x64")

        set(CMAKE_C_COMPILER "/usr/bin/cc")
        set(CMAKE_CXX_COMPILER "/usr/bin/c++")

        set(CMAKE_PKGCONFIG_SYSROOT "/usr/lib/x86_64-linux-gnu/pkgconfig")

    endif()
    
    set(PKGCONFIG_SHARE "${VCPKG_INSTALLED_DIR}/${VCPKG_TARGET_TRIPLET}/share/pkgconfig")

    file(MAKE_DIRECTORY "${PKGCONFIG_SHARE}")
    file(GLOB files "${CMAKE_PKGCONFIG_SYSROOT}/*.pc")
    foreach(file ${files})
        configure_file(${file} ${PKGCONFIG_SHARE} COPYONLY)
    endforeach()

elseif(CMAKE_TARGET_OS STREQUAL "windows")

endif()

# message("-------------------------- Toolchain envs --------------------------")

# get_cmake_property(_variableNames VARIABLES)
# list (SORT _variableNames)
# foreach (_variableName ${_variableNames})
#     message("${_variableName}=${${_variableName}}")
# endforeach()

# message("--------------------------------------------------------------------")

# message("Overlay toolchain done: ${CMAKE_TARGET_ARCH}-${CMAKE_TARGET_OS}")
