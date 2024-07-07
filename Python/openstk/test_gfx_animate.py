import quaternion as quat, numpy as np
from unittest import TestCase, main
from gfx_animate import Bone, ISkeleton, ChannelAttribute, Frame, IAnimation, FrameCache, AnimationController
from openstk.util import _np_createFromQuaternion4x4, _np_createTranslation4x4

# TestBone
class TestBone(Bone, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(1, 'name', np.ones(3), quat.quaternion())
        
    def test__init__(self):
        self.assertEqual(1, self.index)
        self.assertEqual('name', self.name)
        self.assertEqual('[1. 1. 1.]', np.array_str(self.position))
        self.assertEqual(0., self.angle.x)
        self.assertEqual('[[1. 0. 0. 0.]\n [0. 1. 0. 0.]\n [0. 0. 1. 0.]\n [1. 1. 1. 1.]]', np.array_str(self.bindPose))
        self.assertEqual('[1. 0. 0. 0.]', np.array_str(self.inverseBindPose[0]))
        self.assertEqual('[0. 1. 0. 0.]', np.array_str(self.inverseBindPose[1]))
        self.assertEqual('[0. 0. 1. 0.]', np.array_str(self.inverseBindPose[2]))
        self.assertEqual('[-1. -1. -1.  1.]', np.array_str(self.inverseBindPose[3]))
    def test_setParent(self):
        parent = Bone(1, 'name', np.ones(3), quat.quaternion(0))
        # test
        self.setParent(parent)
        self.assertEqual(1, len(parent.children))

# TestSkeleton
class TestSkeleton(ISkeleton):
    Skeleton: ISkeleton
    bones: list[Bone] = [Bone(0, 'bone', np.ones(3), quat.quaternion())]
    root: list[Bone] = [bones[0]]
TestSkeleton.Skeleton = TestSkeleton()

# TestFrame
class TestFrame(Frame, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(TestSkeleton.Skeleton)
        
    def test__init__(self):
        self.assertEqual(1, len(self.bones))
        self.assertEqual(1., self.bones[0].position[0])
        self.assertEqual(0., self.bones[0].angle.x)
        self.assertEqual(1., self.bones[0].scale)
    def test_setAttribute(self):
        self.setAttribute(0, ChannelAttribute.Position, np.ones(3))
        self.setAttribute(0, ChannelAttribute.Angle, quat.quaternion())
        self.setAttribute(0, ChannelAttribute.Scale, 0.)
        self.assertEqual(1., self.bones[0].position[0])
        self.assertEqual(0., self.bones[0].angle.x)
        self.assertEqual(0., self.bones[0].scale)
    def test_clear(self):
        self.clear(TestSkeleton.Skeleton)
        self.assertEqual(1., self.bones[0].position[0])
        self.assertEqual(0., self.bones[0].angle.x)
        self.assertEqual(1., self.bones[0].scale)

# TestAnimation
class TestAnimation(IAnimation):
    Animation: IAnimation
    name: str = 'Animation'
    fps: float = 15.
    frameCount: int = 1
    def decodeFrame(self, index: int, outFrame: Frame) -> None: pass
    def getAnimationMatrices(self, frameCache: FrameCache, index: int | float, skeleton: ISkeleton) -> np.ndarray: return None
TestAnimation.Animation = TestAnimation()

# TestFrameCache
class TestFrameCache(FrameCache, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(TestSkeleton.Skeleton)
        
    def test__init__(self):
        self.assertEqual((-1, 1), (self.previousFrame.frameIndex, len(self.previousFrame.frame.bones)))
        self.assertEqual((-1, 1), (self.nextFrame.frameIndex, len(self.nextFrame.frame.bones)))
        self.assertEqual(1, len(self.interpolatedFrame.bones))
        self.assertEqual(TestSkeleton.Skeleton, self.skeleton)
    def test_clear(self):
        self.clear()
        self.assertEqual((-1, 1), (self.previousFrame.frameIndex, len(self.previousFrame.frame.bones)))
        self.assertEqual((-1, 1), (self.nextFrame.frameIndex, len(self.nextFrame.frame.bones)))
    def test_getFrame(self):
        actual1 = self.getFrame(TestAnimation.Animation, 1.)
        actual2 = self.getFrame(TestAnimation.Animation, 1)
        self.assertTrue(actual1 != None)
        self.assertTrue(actual2 != None)

# TestAnimationController
class TestAnimationController(AnimationController, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(TestSkeleton.Skeleton)
        
    def test__init__(self):
        self.assertTrue(self.frameCache != None)
        pass
    def test_frame(self):
        pass
    def test_update(self):
        pass
    def test_setAnimation(self):
        pass
    def test_pauseLastFrame(self):
        pass
    def test_getAnimationMatrices(self):
        pass
    def test_registerUpdateHandler(self):
        pass

if __name__ == "__main__":
    main(verbosity=1)