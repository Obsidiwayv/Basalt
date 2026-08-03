#pragma once

#if __APPLE__
#define EXPORT __attribute__((visibility("default")))
#elif _WIN32
#define EXPORT __declspec(dllexport)
#endif
#define TESTING_INTEGER 0

EXPORT int ExportThis();
