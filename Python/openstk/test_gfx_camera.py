import numpy as np
from unittest import TestCase, main
from gfx_camera import Camera

# TestCamera
class TestCamera(Camera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertAlmostEqual(-0.6154797, self.pitch)
        self.assertAlmostEqual(-2.3561945, self.yaw)
    def test__recalculateMatrices(self):
        self.setViewportSize(100, 100)
        # test
        self._recalculateMatrices()
        self.assertAlmostEqual(-1.70710671, self.viewProjectionMatrix[0, 0], places=6)
        self.assertAlmostEqual(-0.985598564, self.viewProjectionMatrix[0, 1], places=6)
        self.assertAlmostEqual(-0.577364743, self.viewProjectionMatrix[0, 2], places=6)
        self.assertAlmostEqual(-0.5773503, self.viewProjectionMatrix[0, 3], places=6)
        self.assertAlmostEqual(0.732069254, self.viewProjectionMatrix[3, 2], places=6)
        self.assertAlmostEqual(1.7320509, self.viewProjectionMatrix[3, 3], places=6)
    def test__getForwardVector(self):
        actual = self._getForwardVector()
        self.assertAlmostEqual(-0.577350259, actual[0])
        self.assertAlmostEqual(-0.577350259, actual[1])
        self.assertAlmostEqual(-0.577350259, actual[2])
    def test__getRightVector(self):
        actual = self._getRightVector()
        self.assertAlmostEqual(-0.707107, actual[0])
        self.assertAlmostEqual(0.7071066, actual[1])
        self.assertAlmostEqual(0., actual[2])
    def test_setViewportSize(self):
        self.setViewportSize(100, 100)
        self.assertEqual(1., self.aspectRatio)
        self.assertEqual('[100 100]', np.array_str(self.windowSize))
        self.assertAlmostEqual(2.41421342, self.projectionMatrix[0, 0])
    def test__setViewport(self):
        self._setViewport(0, 0, 100, 100)
    def test_copyFrom(self):
        otherCamera: Camera = TestCamera('test_copyFrom')
        otherCamera.aspectRatio = .5
        # test
        self.copyFrom(otherCamera)
        self.assertEqual(0.5, self.aspectRatio)
    def test_setLocation(self):
        self.setLocation(np.array([1., 1., 1.]))
        self.assertEqual('[1. 1. 1.]', np.array_str(self.location))
    def test_setLocationPitchYaw(self):
        self.setLocationPitchYaw(np.array([1., 1., 1.]), 2., 3.)
        self.assertEqual('[1. 1. 1.]', np.array_str(self.location))
        self.assertEqual(2., self.pitch)
        self.assertEqual(3., self.yaw)
    def test_lookAt(self):
        self.lookAt(np.array([.5, .5, .5]))
        self.assertAlmostEqual(-0.6154797, self.pitch)
        self.assertAlmostEqual(-2.3561945, self.yaw)
    def test_setFromTransformMatrix(self):
        matrix = np.identity(4)
        self.setFromTransformMatrix(np.identity(4))
        self.assertEqual('[0. 0. 0.]', np.array_str(self.location))
        self.assertAlmostEqual(0., self.pitch)
        self.assertAlmostEqual(0., self.yaw)
    def test_setScale(self):
        self.setScale(1.)
        self.assertEqual(1., self.scale)
    def test_tick(self):
        self.tick(1.)
    def test__clampRotation(self):
        self._clampRotation()

if __name__ == "__main__":
    main(verbosity=1)