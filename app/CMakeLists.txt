# For more information about using CMake with Android Studio, read the
# documentation: https://d.android.com/studio/projects/add-native-code.html

# Sets the minimum version of CMake required to build the native library.

cmake_minimum_required(VERSION 3.4.1)

# OBOE

set (OBOE_DIR /home/lzanol/AndroidStudioProjects/oboe)
# add dir to project
add_subdirectory (${OBOE_DIR} ./oboe)
# header files location
include_directories (${OBOE_DIR}/include)

set (APP_DIR src/main/cpp)
file (GLOB_RECURSE APP_SOURCES
    ${APP_DIR}/trace.cpp
    ${APP_DIR}/jni_bridge.cpp
    ${APP_DIR}/PlayAudioEngine.cpp
    ${APP_DIR}/SineGenerator.cpp)

# Creates and names a library, sets it as either STATIC
# or SHARED, and provides the relative paths to its source code.
# You can define multiple libraries, and CMake builds them for you.
# Gradle automatically packages shared libraries with your APK.

add_library(audio-engine SHARED ${APP_SOURCES})

# Searches for a specified prebuilt library and stores the path as a
# variable. Because CMake includes system libraries in the search path by
# default, you only need to specify the name of the public NDK library
# you want to add. CMake verifies that the library exists before
# completing its build.

find_library( # Sets the name of the path variable.
              log-lib

              # Specifies the name of the NDK library that
              # you want CMake to locate.
              log)

# Specifies libraries CMake should link to your target library. You
# can link multiple libraries, such as libraries you define in this
# build script, prebuilt third-party libraries, or system libraries.
# Also links the target library to the log library included in the NDK.

target_link_libraries(audio-engine ${log-lib} oboe)

# Enable optimization flags: if having problems with source level debugging,
# disable -Ofast ( and debug ), re-enable after done debugging.

# target_compile_options(audio-engine PRIVATE -Wall -Werror "$<$<CONFIG:RELEASE>:-Ofast>")
