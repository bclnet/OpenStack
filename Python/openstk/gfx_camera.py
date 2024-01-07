import numpy as np
from .gfr_frustum import Frustum

CAMERASPEED = 300 # Per second
FOV = 0.7853982 # MathX.PiOver4

class Camera:
    location: np.ndarray
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

    def __init__(self, min: np.ndarray, max: np.ndarray):
        self.min = min
        self.max = max
    