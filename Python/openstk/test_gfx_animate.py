import sys, unittest
from gfx_animate import Bone, ISkeleton, ChannelAttribute, FrameBone, Frame, IAnimation, FrameCache, AnimationController

# TestGlCamera
class TestGlCamera(unittest.TestCase):
    camera: GLCamera = GLCamera()
    def should_setViewport(self):
        actual = camera.setViewport(0, 0, 100, 100)
        self.assertTrue(actual != None)

# GLDebugCamera
class GLDebugCamera(unittest.TestCase):
    camera: GLDebugCamera = GLDebugCamera()
    def should_tick(self):
        camera.tick(1.)
    def should_handleInput(self):
        camera.handleInput({}, {})
    def should__handleInputTick(self):
        camera._handleInputTick(1.)


# @unittest.skipUnless(sys.platform.startswith("xwin"), "Requires Windows")
# class GlView(unittest.TestCase):
#     def test_zero(self):
#         self.assertEqual(abs(0), 0)

if __name__ == "__main__":
    unittest.main(verbosity=2)