import math, numpy as np
from OpenGL.GL import *
from openstk.gfx_ui import Key, KeyboardState, MouseState
from openstk.gfx_camera import Camera

CAMERASPEED = 300 # Per second

# GLCamera
class GLCamera(Camera):
    def __init__(self):
        super().__init__()
    def setViewport(self, x: int, y: int, width: int, height: int) -> None:
        print(f'setViewport: {width}, {height}')
        return glViewport(0, 0, width, height)

# GLDebugCamera
class GLDebugCamera(GLCamera):
    mouseOverRenderArea: bool = False # Set from outside this class by forms code
    mouseDragging: bool = False
    mouseDelta: np.ndarray = np.array([0., 0.])
    mousePreviousPosition: np.ndarray = np.array([0., 0.])
    keyboardState: KeyboardState = KeyboardState()
    mouseState: MouseState = MouseState()
    scrollWheelDelta: int = 0

    def __init__(self):
        super().__init__()

    def tick(self, deltaTime: float) -> None:
        if not self.mouseOverRenderArea: return

        # use the keyboard state to update position
        self._handleInputTick(deltaTime)

        # full width of the screen is a 1 PI (180deg)
        self.yaw -= math.pi * self.mouseDelta[0] / self.windowSize[0]
        self.pitch -= math.pi / self.aspectRatio * self.mouseDelta[1] / self.windowSize[1]
        self._clampRotation()
        self._recalculateMatrices()

    def handleInput(self, mouseState: MouseState, keyboardState: KeyboardState) -> None:
        self.scrollWheelDelta += mouseState.scrollWheelValue - self.mouseState.scrollWheelValue
        self.mouseState = mouseState
        self.keyboardState = keyboardState
        if self.mouseOverRenderArea or mouseState.leftButton:
            self.mouseDragging = False
            self.mouseDelta = np.array([0., 0.])
            if not self.mouseOverRenderArea: return

        # drag
        if mouseState.leftButton:
            if not self.mouseDragging: self.mouseDragging = True; self.mousePreviousPosition = np.array([mouseState.x, mouseState.y])
            mouseNewCoords = np.array([mouseState.x, mouseState.y])
            self.mouseDelta[0] = mouseNewCoords[0] - self.mousePreviousPosition[0]
            self.mouseDelta[1] = mouseNewCoords[1] - self.mousePreviousPosition[1]
            self.mousePreviousPosition = mouseNewCoords

    def _handleInputTick(self, deltaTime: float):
        speed = CAMERASPEED * deltaTime

        # double speed if shift is pressed
        if self.keyboardState.isKeyDown(Key.ShiftLeft): speed *= 2
        elif self.keyboardState.isKeyDown(Key.F): speed *= 10

        if self.keyboardState.isKeyDown(Key.W): self.location += self.getForwardVector() * speed
        if self.keyboardState.isKeyDown(Key.S): self.location -= self.getForwardVector() * speed
        if self.keyboardState.isKeyDown(Key.D): self.location += self.getRightVector() * speed
        if self.keyboardState.isKeyDown(Key.A): self.location -= self.getRightVector() * speed
        if self.keyboardState.isKeyDown(Key.Z): self.location += np.array([0., 0., -speed])
        if self.keyboardState.isKeyDown(Key.Q): self.location += np.array([0., 0., speed])

        # scroll
        if self.scrollWheelDelta: self.location += self.getForwardVector() * self.scrollWheelDelta * speed; self.scrollWheelDelta = 0