from numpy import ndarray, array
from enum import IntEnum

# types
type Vector3 = ndarray
type Matrix4x4 = ndarray

class Plane:
    def __init__(self, normal: Vector3, d: float):
        self.normal = normal
        self.d = d

class Point:
    def __init__(self, x: int, y: int):
        self.x = x
        self.y = y
Point.empty = Point(0, 0)

class Point3D:
    def __init__(self, x: int, y: int, z: int):
        self.x = x
        self.y = y
        self.z = z
Point3D.empty = Point3D(0, 0, 0)

class Rectangle:
    def __init__(self, x: int, y: int, width: int, height: int):
        self.x = x
        self.y = y
        self.width = width
        self.height = height
Rectangle.empty = Rectangle(0, 0, 0, 0)

class BoundingBox:
    def __init__(self, min: Vector3, max: Vector3):
        self.min = min
        self.max = max

class BoundingSphere:
    def __init__(self, center: Vector3, radius: float):
        self.center = center
        self.radius = radius

class BoundingFrustum:
    def __init__(self, frustum: Matrix4x4):
        self.frustum = frustum

class Ray:
    def __init__(self, position: Vector3, direction: Vector3):
        self.position = position
        self.direction = direction

class Curve:
    class LoopType(IntEnum):
        Constant = 0
        Cycle = 0
        CycleOffset = 0
        Oscillate = 0
        Linear = 0
    class Continuity(IntEnum):
        Smooth = 0
        Step = 0
    class Key:
        def __init__(self, position: float, value: float, tangentIn: float, tangentOut: float, continuity: int):
            self.position = position
            self.value = value
            self.tangentIn = tangentIn
            self.tangentOut = tangentOut
            self.continuity = Continuity(continuity)
    def __init__(self, preLoop: int, postLoop: int, keys: list[Key]):
        self.preLoop = LoopType(preLoop)
        self.postLoop = LoopType(postLoop)
        self.keys = keys