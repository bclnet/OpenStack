import sys
from unittest import TestCase, main
from gl_camera import GLCamera, GLDebugCamera

# TestGlCamera
class TestGlCamera(GlCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        pass
    def test_setViewport(self):
        self.setViewport(0, 0, 100, 100)

# TestGLDebugCamera
class TestGLDebugCamera(GLDebugCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        pass
    def test_tick(self):
        self.tick(1.)
    def test_handleInput(self):
        self.handleInput({}, {})
    def test__handleInputTick(self):
        self._handleInputTick(1.)

# @unittest.skipUnless(sys.platform.startswith("xwin"), "Requires Windows")
# class GlView(TestCase):
#     def test_zero(self):
#         self.assertEqual(abs(0), 0)

if __name__ == "__main__":
    main(verbosity=2)