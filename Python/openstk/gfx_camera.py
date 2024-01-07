import numpy as np
from openstk.gfx_render import Frustum, IPickingTexture

PiOver2 = 1.570796
CAMERASPEED = 300 # Per second
FOV = 0.7853982 # MathX.PiOver4

class Camera: pass

# Camera
class Camera:
    location: np.ndarray = np.ones(3)
    pitch: float
    yaw: float
    scale: float = 1.
    projectionMatrix: np.matrix
    cameraViewMatrix: np.matrix
    viewProjectionMatrix: np.matrix
    viewFrustum: Frustum = Frustum()
    picker: IPickingTexture
    _windowSize: np.ndarray
    _aspectRatio: float

    def __init__(self):
        self.lookAt(np.zeros(3))

    def _recalculateMatrices(self) -> None:
        self.cameraViewMatrix = Matrix4x4.createScale(self.scale) * Matrix4x4.createLookAt(self.location, self.location + self.getForwardVector(), Vector3.UnitZ)
        self.viewProjectionMatrix = self.cameraViewMatrix * self.projectionMatrix
        self.viewFrustum.update(self.viewProjectionMatrix)

    def _getForwardVector(self) -> np.ndarray: return np.array([(math.cos(self.yaw) * math.cos(self.pitch)), (math.sin(self.yaw) * math.cos(self.pitch)), math.sin(self.pitch)])

    def _getRightVector(self) -> np.ndarray: return np.array([math.cos(self.yaw - PiOver2), math.sin(self.yaw - PiOver2), 0.])

    def setViewportSize(self, viewportWidth: int, viewportHeight: int):
        # store window size and aspect ratio
        self._aspectRatio = viewportWidth / viewportHeight
        self._windowSize = np.array([viewportWidth, viewportHeight])

        # calculate projection matrix
        self.projectionMatrix = Matrix4x4.createPerspectiveFieldOfView(FOV, self._aspectRatio, 1., 40000.)

        self.recalculateMatrices()

        # setup viewport
        self.setViewport(0, 0, viewportWidth, viewportHeight)

        if self.picker: self.picker.resize(viewportWidth, viewportHeight)

    def setViewport(self, x: int, y: int, width: int, height: int) -> None: pass

    def copyFrom(self, fromOther: Camera) -> None:
        self._aspectRatio = fromOther._aspectRatio
        self._windowSize = fromOther._windowSize
        self.location = fromOther.location
        self.pitch = fromOther.pitch
        self.yaw = fromOther.yaw
        self.projectionMatrix = fromOther.projectionMatrix
        self.cameraViewMatrix = fromOther.cameraViewMatrix
        self.viewProjectionMatrix = fromOther.viewProjectionMatrix
        self.viewFrustum.Update(self.viewProjectionMatrix)
    
    def setLocation(self, location: np.ndarray) -> None:
        self.location = location
        self._recalculateMatrices()

    def setLocationPitchYaw(self, location: np.ndarray, pitch: float, yaw: float) -> None:
        self.location = location
        self.pitch = pitch
        self.yaw = yaw
        self._recalculateMatrices()

    def lookAt(self, target: np.ndarray) -> None:
        dir = Vector3.normalize(target - self.location)
        self.yaw = math.atan2(dir.Y, dir.X)
        self.pitch = math.Asin(dir.Z)
        self.clampRotation()
        self.recalculateMatrices()

    def setFromTransformMatrix(self, matrix: np.matrix) -> None:
        self.location = matrix.translation

        # extract view direction from view matrix and use it to calculate pitch and yaw
        dir = Vector3(matrix[0,0], matrix[0,1], matrix[0,2])
        self.yaw = math.atan2(dir.Y, dir.X)
        self.pitch = math.asin(dir.Z)

        self.recalculateMatrices()

    def setScale(self, scale: float) -> None:
        self.scale = scale
        self.recalculateMatrices()

    def tick(self, deltaTime: float) -> None: pass

    # prevent camera from going upside-down
    def _clampRotation(self) -> None:
        if self.pitch >= PiOver2: self.pitch = PiOver2 - 0.001
        elif self.pitch <= -PiOver2: self.pitch = -PiOver2 + 0.001
