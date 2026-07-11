#include "TestHeader.hpp"
#include "glad/gl.h"
#include "GLFW/glfw3.h"
#include <gl/gl.h>

int main() {
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 4);
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 0);
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);

    GLFWwindow* Win = glfwCreateWindow(600, 600, "the", NULL, NULL);

    glfwMakeContextCurrent(Win);

    if (!gladLoadGL(glfwGetProcAddress)) {
        return 0;
    }

    while (!glfwWindowShouldClose(Win)) {
        glClearColor(0, 0, 1.0f, 0);
        glClear(GL_COLOR_BUFFER_BIT);

        glfwSwapBuffers(Win);
        glfwPollEvents();
    }

    glfwTerminate();
    return ExportThis();
}
