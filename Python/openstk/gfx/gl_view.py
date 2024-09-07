import sys, os, time
from OpenGL.GL import *
from OpenGL.raw.GL.EXT.texture_filter_anisotropic import GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT
from PyQt6.QtWidgets import QWidget
from PyQt6.QtCore import Qt, QTimer
from PyQt6.QtGui import QSurfaceFormat, QPainter, QColor
from PyQt6.QtOpenGLWidgets import QOpenGLWidget
from PyQt6.QtOpenGL import QOpenGLBuffer, QOpenGLShader, QOpenGLShaderProgram, QOpenGLTexture, QOpenGLDebugLogger, QOpenGLDebugMessage
from openstk.gfx.gfx import PlatformStats
from openstk.gfx.gfx_ui import MouseState, KeyboardState
from openstk.gfx.gl_camera import GLCamera, GLDebugCamera

# https://forum.qt.io/topic/137468/a-few-basic-changes-in-pyqt6-and-pyside6-regarding-shader-based-opengl-graphics
# https://github.com/8Observer8/falling-collada-cube-bullet-physics-opengl33-pyqt6/blob/master/main.py
# https://stackoverflow.com/questions/50855257/when-to-use-paintevent-and-paintgl-in-qt
# https://codebrowser.dev/qt5/qtbase/src/widgets/kernel/qopenglwidget.cpp.html#_ZN13QOpenGLWidget10paintEventEP11QPaintEvent

def debuggerMessage(msg: QOpenGLDebugMessage) -> None:
    if msg.severity().value >= QOpenGLDebugMessage.Severity.LowSeverity.value: return
    print(f'OpenGL: {msg.type().name[:-4]}:{msg.severity().name[:-8]} - {msg.message()}')

# OpenGLView
class OpenGLView(QOpenGLWidget):
    timer: QTimer
    camera: GLCamera
    viewportChanged: bool = False
    deltaTime: int = 0
    elapsedTime: int

    def __init__(self):
        super().__init__()
        self.setAutoFillBackground(False)
        self.setUpdateBehavior(self.UpdateBehavior.PartialUpdate)
        self.elapsedTime = time.time()
        interval = 0 # 1.0
        if interval:
            self.timer = QTimer(self)
            self.interval = interval
            self.timer.timeout.connect(self.timerTick)
            self.timer.start()

    # tag::Events[]
    def timerTick(self): self.update()
    def resizeGL(self, width: int, height: int) -> None: self.viewportChanged = True; return super().resizeGL(width, height)
    def enterEvent(self, event: object) -> None: self.camera.event(GLCamera.EventType.MouseEnter, event, None); return super().enterEvent(event)
    def leaveEvent(self, event: object) -> None: self.camera.event(GLCamera.EventType.MouseLeave, event, None); return super().leaveEvent(event)
    def mouseMoveEvent(self, event: object) -> None: self.camera.event(GLCamera.EventType.MouseMove, event, event.pos()); return super().mouseMoveEvent(event)
    def mousePressEvent(self, event: object) -> None: button = event.button(); self.camera.event(GLCamera.EventType.MouseDown, event, (Qt.MouseButton.LeftButton in button, Qt.MouseButton.RightButton in button)); return super().mousePressEvent(event)
    def keyPressEvent(self, event: object) -> None: self.camera.event(GLCamera.EventType.KeyPress, event, event.key()); return super().keyPressEvent(event)
    def keyReleaseEvent(self, event: object) -> None: self.camera.event(GLCamera.EventType.KeyRelease, event, event.key()); return super().keyReleaseEvent(event)
    # end::Events[]

    # tag::Tick[]
    def tick(self, **kwargs) -> None:
        deltaTime: int = kwargs.get('deltaTime', None)
        if not deltaTime: elapsedTime = time.time(); self.deltaTime = self.elapsedTime = elapsedTime - self.elapsedTime; self.elapsedTime = elapsedTime
        else: self.deltaTime = deltaTime
        mouseState = self.camera.mouseState; keyboardState = self.camera.keyboardState
        self.camera.tick(self.deltaTime)
        self.camera.handleInput(mouseState, keyboardState)
        self.handleInput(mouseState, keyboardState)

    def handleInput(self, mouseState: MouseState, keyboardState: KeyboardState) -> None: pass
    # end::Tick[]

    # tag::Render[]
    def setViewport(self, x: int, y: int, width: int, height: int) -> None: self.viewportChanged = False; self.camera.setViewport(x, y, width, height)

    # def paintEvent(self, event: object):
    #     p: QPainter = QPainter(self)
    #     p.beginNativePainting()
    #     self.paintGL()
    #     p.endNativePainting()
    #     self.paint(p)

    # https://stackoverflow.com/questions/38796140/qpainterdrawrects-painter-not-active-error-c-qt
    # def paint(self, p: QPainter):
    #     r1: QRect = self.rect().adjusted(10, 10, -10, -10)
    #     p.setPen(QColor("#FFFFFF"))
    #     p.drawRect(r1)
    #     #
    #     # r2: QRect = QRect(QPoint(0, 0), QSize(100, 100))
    #     # r2.moveCenter(m_mousePos)
    #     # p.setPen(QPen(Qt.black, 3, Qt.SolidLine, Qt.SquareCap, Qt.MiterJoin))
    #     # p.drawRect(r2)

    def paintGL(self):
        if self.viewportChanged: self.setViewport(0, 0, self.width(), self.height())
        self.tick()
        self.renderGL()
        # ctx = self.context(); ctx.swapBuffers(ctx.surface())
        
    def renderGL(self):
        glClearColor(0.2, 0.3, 0.3, 1.)
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        self.render(self.camera, self.deltaTime)

    def render(self, camera: GLCamera, deltaTime: float): pass
    # end::Render[]

    # tag::InitializeGL[]
    def initializeGL(self):
        super().initializeGL()
        self.makeCurrent()
        self.debugger = QOpenGLDebugLogger(self)
        self.checkGL()
        self.initGL()
        self.camera = GLDebugCamera()
        # glEnable(GL_DEPTH_TEST)

    checkGLCalled: bool = False
    def checkGL(self):
        if OpenGLView.checkGLCalled == True: return
        OpenGLView.checkGLCalled = True
        format = QSurfaceFormat.defaultFormat()
        print(f'QSurface format: {format.version()}')
        print(f'OpenGL version: {glGetString(GL_VERSION).decode()}')
        print(f'OpenGL vendor: {glGetString(GL_VENDOR).decode()}')
        if self.debugger.initialize():
            print(f'OpenGL debugger: installed')
            self.debugger.messageLogged.connect(debuggerMessage)
            self.debugger.startLogging()
        print(f'GLSL version: {glGetString(GL_SHADING_LANGUAGE_VERSION).decode()}')
        extensions = {}
        for i in range(glGetInteger(GL_NUM_EXTENSIONS)):
            extension = glGetStringi(GL_EXTENSIONS, i).decode()
            if extension not in extensions: extensions[extension] = None
        if 'GL_EXT_texture_filter_anisotropic' in extensions:
            maxTextureMaxAnisotropy = glGetInteger(GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT)
            PlatformStats.maxTextureMaxAnisotropy = maxTextureMaxAnisotropy
            print(f'MaxTextureMaxAnisotropyExt: {maxTextureMaxAnisotropy}')
        else: print(f'GL_EXT_texture_filter_anisotropic is not supported')

    def initGL(self): pass
    # end::InitializeGL[]
