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

#region Vanilla3dWidget

class Vanilla3dWidget(QWidget, ShowBase):
    renderer: Renderer = None
    id: int = 0

    # Binding

    def __init__(self, parent: object, tab: object):
        loadPrcFileData('', """
        allow-parent 1
        window-title GameX
        show-frame-rate-meter #t
        """)
        super(QWidget, self).__init__(parent)
        super(ShowBase, self).__init__()
        self.gfx: IOpenGfx = parent.gfx
        self.sfx: IOpenSfx = parent.sfx
        self.source: object = tab
        self.path: object = parent.path
        self.value: object = tab.value
        self.type: str = tab.type
        # print('win: %s' % base.win.getProperties())

        # self.disableMouse()
        # self.camera.setPos(0, -10, 0)
        # self.camera.lookAt(0, 0, 0)
        self.onSourceChanged()

    def createRenderer(self) -> Renderer: pass
    
    def onSourceChanged(self) -> None:
        if not self.gfx or not self.path or not self.value or not self.type: return
        self.renderer = self.createRenderer()
        if self.renderer: self.renderer.start()
        if isinstance(self.value, ITextureSelect): self.value.select(self.id)

    def closeEvent(self, event):
        self.taskMgr.stop()
        self.closeWindow()
        self.destroy()

    # Render

    def showEvent(self, event: QEvent) -> None:
        super().showEvent(event)
        wp = WindowProperties().getDefault()
        # wp.setForeground(False)
        # wp.setOrigin(0, 0)
        wp.setSize(self.width(), self.height())
        # wp.setParentWindow(int(self.winId()))
        self.openDefaultWindow(props=wp)
        self.run()
        
    def tick(self):
        print('tick')
    #     self.engine.render_frame()
        self.clock.tick()

    def resizeEvent(self, event):
        wp = WindowProperties()
        wp.setParentWindow(int(self.winId()))
        wp.setSize(self.width(), self.height())
        # self.win.requestProperties(wp)
        # self.openDefaultWindow(props=wp)

#endregion
