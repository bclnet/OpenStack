import numpy as np
from OpenGL import GL as gl
from openstk.gfx_render import Camera

# GLCamera
class GLCamera(Camera):
    def _setViewport(x: int, y: int, width: int, height: int) -> None:
        return gl.glViewport(0, 0, width, height)

# GLDebugCamera
class GLDebugCamera(GLCamera):
    mouseOverRenderArea: bool # Set from outside this class by forms code
    mouseDragging: bool
    mouseDelta: np.ndarray
    mousePreviousPosition: np.ndarray
    keyboardState: object #KeyboardState
    mouseState: object #MouseState
    scrollWheelDelta: int

    def tick(self, deltaTime: float) -> None:
        if not self.mouseOverRenderArea: return
        # Use the keyboard state to update position
        self.handleInputTick(deltaTime)
        # Full width of the screen is a 1 PI (180deg)
        self.yaw -= math.PI * self.mouseDelta.X / self.windowSize.X
        self.pitch -= math.PI / self._aspectRatio * self.mouseDelta.Y / self.windowSize.Y
        self.clampRotation()
        self.recalculateMatrices()

    def handleInput(self, mouseState: object, keyboardState: object): # MouseState, KeyboardState
        self.scrollWheelDelta += mouseState.scrollWheelValue - self.mouseState.scrollWheelValue
        self.mouseState = mouseState
        self.keyboardState = keyboardState
        if self.mouseOverRenderArea or mouseState.leftButton == ButtonState.Released:
            self.mouseDragging = False
            self.mouseDelta = default
            if not self.mouseOverRenderArea: return

        # drag
        if mouseState.leftButton == ButtonState.Pressed:
            if not self.mouseDragging:
                self.mouseDragging = True
                self.mousePreviousPosition = np.array([mouseState.X, mouseState.Y])
            mouseNewCoords = np.array([mouseState.X, mouseState.Y])
            self.mouseDelta.X = mouseNewCoords.X - self.mousePreviousPosition.X
            self.mouseDelta.Y = mouseNewCoords.Y - self.mousePreviousPosition.Y
            self.mousePreviousPosition = mouseNewCoords

    def handleInputTick(self, deltaTime: float):
        speed = CAMERASPEED * deltaTime

        # double speed if shift is pressed
        if self.keyboardState.IsKeyDown(Key.ShiftLeft): speed *= 2
        elif self.keyboardState.IsKeyDown(Key.F): speed *= 10

        if self.keyboardState.IsKeyDown(Key.W): self.location += self._getForwardVector() * speed
        if self.keyboardState.IsKeyDown(Key.S): self.location -= self._getForwardVector() * speed
        if self.keyboardState.IsKeyDown(Key.D): self.location += self._getRightVector() * speed
        if self.keyboardState.IsKeyDown(Key.A): self.location -= self._getRightVector() * speed
        if self.keyboardState.IsKeyDown(Key.Z): self.location += np.array([0., 0., -speed])
        if self.keyboardState.IsKeyDown(Key.Q): self.location += np.array([0., 0., speed])

        # scroll
        if self.scrollWheelDelta:
            self.location += self._getForwardVector() * self.scrollWheelDelta * speed
            self.scrollWheelDelta = 0