import sys, os, time
from OpenGL import GL as gl
from OpenGL.raw.GL.EXT.texture_filter_anisotropic import GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT
from PyQt6.QtWidgets import QWidget
from PyQt6.QtCore import Qt
from PyQt6.QtOpenGLWidgets import QOpenGLWidget
from PyQt6.QtOpenGL import QOpenGLBuffer, QOpenGLShader, QOpenGLShaderProgram, QOpenGLTexture
from openstk.gfx import PlatformStats
from openstk.gl_camera import GLDebugCamera

# https://forum.qt.io/topic/137468/a-few-basic-changes-in-pyqt6-and-pyside6-regarding-shader-based-opengl-graphics
# https://github.com/8Observer8/falling-collada-cube-bullet-physics-opengl33-pyqt6/blob/master/main.py

class OpenGLView(QOpenGLWidget):
    def __init__(self):
        super().__init__()
        self.camera = GLDebugCamera()
        self.paints = []

    def checkGL(self):
        print(f'OpenGL version: {gl.glGetString(gl.GL_VERSION).decode()}');
        print(f'OpenGL vendor: {gl.glGetString(gl.GL_VENDOR).decode()}');
        print(f'GLSL version: {gl.glGetString(gl.GL_SHADING_LANGUAGE_VERSION).decode()}');

        extensions = {}
        for i in range(gl.glGetInteger(gl.GL_NUM_EXTENSIONS)):
            extension = gl.glGetStringi(gl.GL_EXTENSIONS, i).decode()
            if extension not in extensions: extensions[extension] = None

        if 'GL_EXT_texture_filter_anisotropic' in extensions:
            maxTextureMaxAnisotropy = gl.glGetInteger(GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT)
            PlatformStats.maxTextureMaxAnisotropy = maxTextureMaxAnisotropy
            print(f'MaxTextureMaxAnisotropyExt: {maxTextureMaxAnisotropy}')
        else:
            print(f'GL_EXT_texture_filter_anisotropic is not supported')

    def initializeGL(self):
        self.checkGL()
        self.elapsedTime = time.time()
        self._handleResize(self.width(), self.height())
    
    def resizeGL(self, width: int, height: int):
        self._handleResize(width, height)

    def enterEvent(self, event):
        self.camera.mouseOverRenderArea = True
        return super().enterEvent(event)

    def leaveEvent(self, event):
        self.camera.mouseOverRenderArea = False
        return super().leaveEvent(event)

    def paintGL(self):
        elapsedTime = time.time()
        self.elapsedTime = elapsedTime - self.elapsedTime
        frameTime = self.elapsedTime / 1000.
        self.elapsedTime = elapsedTime

        self.camera.tick(frameTime)
        # self.camera.handleInput(None, None) #OpenTK.Input.Mouse.GetState(), OpenTK.Input.Keyboard.GetState()

        gl.glClearColor(0.2, 0.3, 0.3, 1.)
        gl.glClear(gl.GL_COLOR_BUFFER_BIT | gl.GL_DEPTH_BUFFER_BIT)

        for paint in self.paints:
            paint(frameTime, self.camera)

        # self.update()

    def _handleResize(self, width: int, height: int):
        self.camera.setViewportSize(width, height)
        self._recalculatePositions()

    def _recalculatePositions(self):
        pass