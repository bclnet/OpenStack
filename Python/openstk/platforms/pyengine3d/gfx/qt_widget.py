import sys, os, numpy as np
from PyQt6.QtCore import Qt, QEvent, QTimer, QElapsedTimer
from PyQt6.QtGui import QWindow
from PyQt6.QtWidgets import QWidget, QVBoxLayout, QHBoxLayout, QPushButton
from openstk.gfx import ITextureSelect, MouseState, KeyboardState

# typedefs
class Renderer: pass
class EginRenderer: pass
class Camera: pass
class IOpenGfx: pass
class IOpenSfx: pass

#region PyEngine3dWidget

class PyEngine3dWidget(QWidget):
    renderer: Renderer = None
    id: int = 0

    # Binding

    def __init__(self, parent: object, tab: object):
        super().__init__()
        self.gfx: list[IOpenGfx] = parent.gfx
        self.sfx: list[IOpenSfx] = parent.sfx
        self.source: object = tab
        self.path: object = parent.path
        self.value: object = tab.value
        self.type: str = tab.type

        self.onSourceChanged()

    def createRenderer(self) -> Renderer: pass
    
    def onSourceChanged(self) -> None:
        if not self.gfx or not self.path or not self.value or not self.type: return
        self.renderer = self.createRenderer()
        if self.renderer: self.renderer.start()
        if isinstance(self.value, ITextureSelect): self.value.select(self.id)

    # Render

    def resizeEvent(self, event):
        print('resizeEvent')

    def tick(self):
        print('tick')

#endregion
