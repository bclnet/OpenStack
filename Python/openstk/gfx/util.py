import math, quaternion as quat, numpy as np
# https://realpython.com/python-unittest/

@staticmethod
def _throw(message: str) -> None:
    raise Exception(message)

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Vector3.cs#L202
@staticmethod
def _np_normalize(vector: np.ndarray) -> np.ndarray:
    norm = np.linalg.norm(vector)
    return vector / norm if norm else np.zeros(vector.shape[0])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs#L358
@staticmethod
def _np_createTranslation4x4(*args) -> np.ndarray:
    match args:
        case ([position]):
            return np.array([
                [1., 0., 0., 0.],
                [0., 1., 0., 0.],
                [0., 0., 1., 0.],
                [position[0], position[1], position[2], 1.]])
        case (xPosition, yPosition, zPosition):
            return np.array([
                [1., 0., 0., 0.],
                [0., 1., 0., 0.],
                [0., 0., 1., 0.],
                [xPosition, yPosition, zPosition, 1.]])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs#L1118
@staticmethod
def _np_createFromQuaternion4x4(quaternion: quat.quaternion) -> np.ndarray:
    xx: float = quaternion.x * quaternion.x
    yy: float = quaternion.y * quaternion.y
    zz: float = quaternion.z * quaternion.z
    xy: float = quaternion.x * quaternion.y
    wz: float = quaternion.z * quaternion.w
    xz: float = quaternion.z * quaternion.x
    wy: float = quaternion.y * quaternion.w
    yz: float = quaternion.y * quaternion.z
    wx: float = quaternion.x * quaternion.w
    return np.array([
        [1. - 2. * (yy + zz), 2. * (xy + wz), 2. * (xz - wy), 0.],
        [2. * (xy - wz), 1. - 2. * (zz + xx), 2. * (yz + wx), 0.],
        [2. * (xz + wy), 2. * (yz - wx), 1. - 2. * (yy + xx), 0.],
        [0., 0., 0., 1.]])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs#L117
@staticmethod
def _np_getTranslation4x4(matrix: np.ndarray) -> np.ndarray:
    return np.array([matrix[3, 0], matrix[3, 1], matrix[3, 2]])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs#L390
@staticmethod
def _np_createScale4x4(scale: float) -> np.ndarray:
    return np.array([
        [scale, 0., 0., 0.],
        [0., scale, 0., 0.],
        [0., 0., scale, 0.],
        [0., 0., 0., 1.]])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs
@staticmethod
def _np_createLookAt4x4(cameraPosition: np.ndarray, cameraTarget: np.ndarray, cameraUpVector: np.ndarray) -> np.ndarray:
    zaxis = _np_normalize(cameraPosition - cameraTarget)
    xaxis = _np_normalize(np.cross(cameraUpVector, zaxis))
    yaxis = np.cross(zaxis, xaxis)
    return np.array([
        [xaxis[0], yaxis[0], zaxis[0], 0.],
        [xaxis[1], yaxis[1], zaxis[1], 0.],
        [xaxis[2], yaxis[2], zaxis[2], 0.],
        [-np.matmul(xaxis, cameraPosition), -np.matmul(yaxis, cameraPosition), -np.matmul(zaxis, cameraPosition), 1.]])
        # [-np.dot(xaxis, cameraPosition), -np.dot(yaxis, cameraPosition), -np.dot(zaxis, cameraPosition), 1.]])

#ref https://github.com/microsoft/referencesource/blob/master/System.Numerics/System/Numerics/Matrix4x4.cs
@staticmethod
def _np_createPerspectiveFieldOfView4x4(fieldOfView: float, aspectRatio: float, nearPlaneDistance: float, farPlaneDistance: float) -> np.ndarray:
    if fieldOfView <= 0. or fieldOfView >= math.pi: raise Exception('fieldOfView')
    if nearPlaneDistance <= 0.: raise Exception('nearPlaneDistance')
    if farPlaneDistance <= 0.: raise Exception('farPlaneDistance')
    if nearPlaneDistance >= farPlaneDistance: raise Exception('nearPlaneDistance')
    yScale = 1. / math.tan(fieldOfView * 0.5)
    xScale = yScale / aspectRatio
    return np.array([
        [xScale, 0., 0., 0.],
        [0., yScale, 0., 0.],
        [0., 0., farPlaneDistance / (nearPlaneDistance - farPlaneDistance), -1.],
        [0., 0., nearPlaneDistance * farPlaneDistance / (nearPlaneDistance - farPlaneDistance), 0.]])