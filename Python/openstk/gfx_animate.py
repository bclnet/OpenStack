import numpy as np
import quaternion
from typing import Any
from enum import Enum
# from openstk.gfx_render import Frustum

print(type(np.quaternion()))

# AnimationController
class AnimationController:
    frameCache: FrameCache
    updateHandler: Any = lambda a, b: None
    activeAnimation: IAnimation
    time: float
    shouldUpdate: bool
    @property
    def activeAnimation(self) -> IAnimation: return self.activeAnimation
    isPaused: bool
    @property
    def frame(self) -> int:
        return round(self.time * activeAnimation.fps) % activeAnimation.frameCount if self.activeAnimation and self.activeAnimation.frameCount != 0 else 0
    @frame.setter
    def setFrame(self, value: int) -> None:
        if activeAnimation:
            self.time = value / activeAnimation.Fps if activeAnimation.Fps != 0 else 0.
            self. shouldUpdate = True

    def __init__(self, skeleton: ISkeleton):
        self.frameCache = FrameCache(skeleton)

    def update(self, timeStep: float) -> bool:
        if not self.activeAnimation: return False
        if self.isPaused:
            res = self.shouldUpdate
            self.shouldUpdate = False
            return res
        self.time += timeStep
        self.updateHandler(activeAnimation, self.frame)
        self.shouldUpdate = False
        return True

    def setAnimation(self, animation: IAnimation) -> None:
        self.frameCache.clear()
        self.activeAnimation = animation
        self.time = 0.
        self.updateHandler(activeAnimation, -1)

    def pauseLastFrame(self) -> None:
        self.isPaused = True
        self.frame = 0 if not self.activeAnimation else activeAnimation.frameCount - 1

    def getAnimationMatrices(self, skeleton: ISkeleton) -> np.matrix:
        return activeAnimation.getAnimationMatrices(self.frameCache, self.frame, skeleton) if self.isPaused else \
            activeAnimation.getAnimationMatrices(self.frameCache, self.time, skeleton);

    def registerUpdateHandler(self, handler: Any) -> None: self.updateHandler = handler

# bone
class Bone:
    index: int
    parent: Bone
    children: list[Bone] = []
    name: str
    position: np.ndarray
    angle: object # Quaternion
    bindPose: np.matrix
    inverseBindPose: np.matrix

    def __init__(self, index: int, name: str, position: np.ndarray, rotation: Quaternion):
        self.index = index
        self.name = name
        self.position = position
        self.angle = rotation
        # Calculate matrices
        # self.bindPose = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position)
        # Matrix4x4.Invert(BindPose, out var inverseBindPose)
        self.inverseBindPose = inverseBindPose

    def setParent(parent: Bone) -> None:
        if self.children.contains(parent):
            self.parent = parent
            parent.children.append(self)

# ChannelAttribute
class ChannelAttribute(Enum):
    Position = 1
    Angle = 2
    Scale = 3
    Unknown = 4

# FrameBone
class FrameBone:
    position: np.ndarray
    angle: Quaternion
    scale: float