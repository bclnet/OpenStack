import sys
from unittest import TestCase, main
from openstk.gfx_ui import KeyboardState, MouseState
from gl_camera import GLCamera, GLDebugCamera

# TestGLCamera
class TestGLCamera(GLCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()
        self.setViewportSize(100, 100)

    def test__init__(self):
        pass
    def test_setViewport(self):
        self.setViewport(0, 0, 100, 100)

# TestGLDebugCamera
class TestGLDebugCamera(GLDebugCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()
        self.mouseOverRenderArea = True
        self.setViewportSize(100, 100)

    def test__init__(self):
        pass
    def test_tick(self):
        self.tick(1.)
    def test_handleInput(self):
        self.handleInput(MouseState(), KeyboardState())
    def test__handleInputTick(self):
        self._handleInputTick(1.)

if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=2)