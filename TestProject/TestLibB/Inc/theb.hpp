#pragma once

#if __APPLE__
#define EXPORT __attribute__((visibility("default")))
#elif
#define EXPORT __declspec(dllexport)
#endif

void EXPORT ExportThatB();
