import sys, os
from OpenGL import GL as gl
from OpenGL.raw.GL.EXT.texture_filter_anisotropic import GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT
from PyQt6.QtWidgets import QWidget
from PyQt6.QtCore import Qt
from PyQt6.QtOpenGLWidgets import QOpenGLWidget
from PyQt6.QtOpenGL import QOpenGLBuffer, QOpenGLShader, QOpenGLShaderProgram, QOpenGLTexture
from openstk.gfx import PlatformStats

# https://forum.qt.io/topic/137468/a-few-basic-changes-in-pyqt6-and-pyside6-regarding-shader-based-opengl-graphics
# https://github.com/8Observer8/falling-collada-cube-bullet-physics-opengl33-pyqt6/blob/master/main.py

class OpenGLView(QOpenGLWidget):
    def __init__(self):
        super().__init__()

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
        gl.glClearColor(0.5, 0.5, 0.5, 1)
    
    def paintGL(self):
        gl.glClear(gl.GL_COLOR_BUFFER_BIT)
