import sys, os, numpy as np
from PyQt6.QtCore import Qt, QEvent, QTimer, QElapsedTimer
from PyQt6.QtGui import QWindow
from PyQt6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QPushButton
from openstk.gfx import ITextureSelect, MouseState, KeyboardState
# opengl
from OpenGL.GL import *
from openstk.gfx.qt_widget.opengl import QOpenGLWidget

# typedefs
class Renderer: pass
class EginRenderer: pass
class Camera: pass
class IOpenGfx: pass
class IOpenSfx: pass

#region OpenGLWidget

# OpenGLWidget
class OpenGLWidget(QOpenGLWidget):
    renderer: EginRenderer = None
    id: int = 0

    # Binding

    def __init__(self, parent: object, tab: object, interval: float = 1.0):
        super().__init__(interval)
        self.gfx: list[IOpenGfx] = parent.gfx
        self.sfx: list[IOpenSfx] = parent.sfx
        self.path: object = parent.path
        self.source: object = tab.value
        self.type: str = tab.type
        
    def createRenderer(self) -> Renderer: pass
    
    def onSourceChanged(self) -> None:
        if not self.gfx or not self.source or not self.type: return
        self.renderer = self.createRenderer()
        if self.renderer: self.renderer.start()
        if isinstance(self.source, ITextureSelect): self.source.select(self.id)

    # Render

    def initializeGL(self) -> None:
        super().initializeGL()
        self.onSourceChanged()

    def setViewport(self, x: int, y: int, width: int, height: int) -> None:
        p = self.renderer.getViewport((width, height)) or (width, height) if self.renderer else (width, height)
        super().setViewport(x, y, p[0], p[1])

    def render(self, camera: Camera, frameTime: float) -> None:
        if self.renderer: self.renderer.render(camera, frameTime)

    def tick(self) -> None:
        super().tick()
        if self.renderer: self.renderer.update(self.deltaTime)
        self.render(self.camera, 0.0)

    # HandleInput

    def handleInput(self, mouseState: MouseState, keyboardState: KeyboardState) -> None: pass

#endregion
