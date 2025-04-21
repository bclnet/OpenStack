from __future__ import annotations
import quaternion as quat, numpy as np
from typing import NamedTuple
from enum import Enum
from openstk.gfx.util import _np_createFromQuaternion4x4, _np_createTranslation4x4

#region Animate

# bone
class Bone:
    index: int
    parent: Bone
    children: list[Bone] = []
    name: str
    position: np.ndarray #Vector3
    angle: quat.quaternion
    bindPose: np.ndarray #Matrix4x4
    inverseBindPose: np.ndarray #Matrix4x4

    def __init__(self, index: int, name: str, position: np.ndarray, rotation: quat.quaternion):
        self.index = index
        self.name = name
        self.position = position
        self.angle = rotation
        # calculate matrices
        self.bindPose = np.matmul(_np_createFromQuaternion4x4(rotation), _np_createTranslation4x4(position))
        self.inverseBindPose = np.linalg.inv(self.bindPose)

    def setParent(self, parent: Bone) -> None:
        if parent not in self.children:
            self.parent = parent
            parent.children.append(self)

# ISkeleton
class ISkeleton:
    roots: list[Bone]
    bones: list[Bone]

# ChannelAttribute
class ChannelAttribute(Enum):
    Position = 0
    Angle = 1
    Scale = 2
    Unknown = 3

# FrameBone
class FrameBone:
    position: np.ndarray #Vector3
    angle: quat.quaternion
    scale: float

# Frame
class Frame:
    bones: list[FrameBone]

    def __init__(self, skeleton: ISkeleton):
        self.bones = [FrameBone]*len(skeleton.bones)
        self.clear(skeleton)

    def setAttribute(self, bone: int, attribute: ChannelAttribute, data: np.ndarray | quat.quaternion | float) -> None:
        match data:
            case p if isinstance(data, np.ndarray):
                match attribute:
                    case ChannelAttribute.Position: self.bones[bone].position = p
#if DEBUG
                    case _: print(f"Unknown frame attribute '{attribute}' encountered with Vector3 data")
#endif
            case q if isinstance(data, quat.quaternion):
                match attribute:
                    case ChannelAttribute.Angle: self.bones[bone].angle = q
#if DEBUG
                    case _: print(f"Unknown frame attribute '{attribute}' encountered with Quaternion data")
#endif
            case f if isinstance(data, float):
                match attribute:
                    case ChannelAttribute.Scale: self.bones[bone].scale = f
#if DEBUG
                    case _: print(f"Unknown frame attribute '{attribute}' encountered with float data")
#endif
            case _: raise Exception(f'Unknown {data}')

    def clear(self, skeleton: ISkeleton) -> None:
        for i in range(len(self.bones)):
            self.bones[i].position = skeleton.bones[i].position
            self.bones[i].angle = skeleton.bones[i].angle
            self.bones[i].scale = 1.

# IAnimation
class IAnimation:
    name: str
    fps: float
    frameCount: int
    def decodeFrame(self, index: int, outFrame: Frame) -> None: pass
    def getAnimationMatrices(self, frameCache: FrameCache, index: int | float, skeleton: ISkeleton) -> np.ndarray: pass

# FrameTuple
class FrameTuple(NamedTuple):
    frameIndex: int
    frame: Frame

# FrameCache
class FrameCache:
    frameFactory: callable = lambda skeleton: Frame(skeleton)

    previousFrame: FrameTuple #(int, Frame)
    nextFrame: FrameTuple #(int, Frame)
    interpolatedFrame: Frame
    skeleton: ISkeleton

    def __init__(self, skeleton: ISkeleton):
        self.previousFrame = FrameTuple(-1, FrameCache.frameFactory(skeleton))
        self.nextFrame = FrameTuple(-1, FrameCache.frameFactory(skeleton))
        self.interpolatedFrame = FrameCache.frameFactory(skeleton)
        self.skeleton = skeleton
        self.clear()

    def clear(self) -> None:
        self.previousFrame = FrameTuple(-1, self.previousFrame.frame); self.previousFrame.frame.clear(self.skeleton)
        self.nextFrame = FrameTuple(-1, self.nextFrame.frame); self.nextFrame.frame.clear(self.skeleton)

    def getFrame(self, anim: IAnimation, index: int | float) -> Frame:
        match index:
            case time if isinstance(index, float):
                # calculate the index of the current frame
                frameIndex = int(time * anim.fps) % anim.frameCount
                t = (time * anim.fps - frameIndex) % 1
                # get current and next frame
                frame1 = self.getFrame(anim, frameIndex)
                frame2 = self.getFrame(anim, (frameIndex + 1) % anim.frameCount)
                # interpolate bone positions, angles and scale
                for i in range(len(frame1.bones)):
                    frame1Bone = frame1.bones[i]
                    frame2Bone = frame2.bones[i]
                    self.interpolatedFrame.bones[i].position = np.interp(t, frame1Bone.position, frame2Bone.position)
                    self.interpolatedFrame.bones[i].angle = quat.slerp_evaluate(frame1Bone.angle, frame2Bone.angle, t)
                    self.interpolatedFrame.bones[i].scale = frame1Bone.scale + (frame2Bone.scale - frame1Bone.scale) * t
                return self.interpolatedFrame
            case frameIndex if isinstance(index, int):
                # try to lookup cached (precomputed) frame - happens when GUI Autoplay runs faster than animation FPS
                if frameIndex == self.previousFrame.frameIndex: return self.previousFrame.frame
                elif frameIndex == self.nextFrame.frameIndex: return self.nextFrame.frame
                # only two frames are cached at a time to minimize memory usage, especially with Autoplay enabled
                frame: Frame
                if frameIndex > self.previousFrame.frameIndex: frame = self.previousFrame.frame; self.previousFrame = self.nextFrame; self.nextFrame = FrameTuple(frameIndex, frame)
                else: frame = self.nextFrame.frame; self.nextFrame = self.previousFrame; self.previousFrame = FrameTuple(frameIndex, frame)
                # we make an assumption that frames within one animation contain identical bone sets, so we don't clear frame here
                anim.decodeFrame(frameIndex, frame)
                return frame
            case _: raise Exception(f'Unknown {data}')

# AnimationController
class AnimationController:
    frameCache: FrameCache
    updateHandler: callable = lambda a, b: None
    activeAnimation: IAnimation
    time: float
    shouldUpdate: bool
    isPaused: bool
    
    @property
    def frame(self) -> int:
        return round(self.time * self.activeAnimation.fps) % self.activeAnimation.frameCount if self.activeAnimation and self.activeAnimation.frameCount != 0 else 0
    @frame.setter
    def setFrame(self, value: int) -> None:
        if self.activeAnimation:
            self.time = value / self.activeAnimation.fps if self.activeAnimation.fps != 0 else 0.
            self.shouldUpdate = True

    def __init__(self, skeleton: ISkeleton):
        self.frameCache = FrameCache(skeleton)

    def update(self, timeStep: float) -> bool:
        if not self.activeAnimation: return False
        if self.isPaused: res = self.shouldUpdate; self.shouldUpdate = False; return res
        self.time += timeStep
        self.updateHandler(self.activeAnimation, self.frame)
        self.shouldUpdate = False
        return True

    def setAnimation(self, animation: IAnimation) -> None:
        self.frameCache.clear()
        self.activeAnimation = animation
        self.time = 0.
        self.updateHandler(self.activeAnimation, -1)

    def pauseLastFrame(self) -> None:
        self.isPaused = True
        self.frame = 0 if not self.activeAnimation else self.activeAnimation.frameCount - 1

    def getAnimationMatrices(self, skeleton: ISkeleton) -> np.ndarray:
        return self.activeAnimation.getAnimationMatrices(self.frameCache, self.frame, skeleton) if self.isPaused else \
            self.activeAnimation.getAnimationMatrices(self.frameCache, self.time, skeleton)

    def registerUpdateHandler(self, handler: callable) -> None: self.updateHandler = handler

#endregion