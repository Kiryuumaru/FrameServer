setup("Frame Server")

find_package(Protobuf REQUIRED)
find_package(OpenCV CONFIG REQUIRED)
    
add_definitions(-DWITH_LIBV4L=ON)
add_definitions(-DBUILD_SHARED_LIBS=OFF)

file(GLOB_RECURSE DOMAIN_SOURCE_FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/domain/*.cpp)
file(GLOB_RECURSE APPLICATION_SOURCE_FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/application/*.cpp)
file(GLOB_RECURSE INFRASTRUCTURE_SOURCE_FILES ${CMAKE_CURRENT_SOURCE_DIR}/src/infrastructure/*.cpp)
list(APPEND SOURCE_FILES ${DOMAIN_SOURCE_FILES} ${APPLICATION_SOURCE_FILES} ${INFRASTRUCTURE_SOURCE_FILES})
    
list(APPEND LINK_LIBS PRIVATE
    viana
    opencv_core
    opencv_videoio
    opencv_highgui
    ${COMMON_LINK_LIBS}
)
list(APPEND LINK_DIRS PRIVATE
    ${COMMON_LINK_DIRS}
)
list(APPEND INCLUDE_DIRS PRIVATE
    ${Boost_INCLUDE_DIRS}
    ${COMMON_INCLUDE_DIRS}
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# cli executable
add_executable(frame_server ${CMAKE_CURRENT_SOURCE_DIR}/src/presentation/main.cpp ${SOURCE_FILES})
target_link_libraries(frame_server ${LINK_LIBS} ${LINK_LIBS})
target_link_directories(frame_server ${LINK_DIRS})
target_include_directories(frame_server ${INCLUDE_DIRS})
