import numpy as np
from unittest import TestCase, main
from gl_render import GLCamera, GLDebugCamera, GLMeshBuffers, GLMeshBufferCache, MeshBatchRenderer, QuadIndexBuffer, GLPickingTexture, GLRenderMaterial, GLRenderableMesh, OctreeDebugRenderer
from gfx_ui import KeyboardState, MouseState

#region Camera

# TestGLCamera
class TestGLCamera(GLCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()
        self.setViewport(0, 0, 100, 100)
    def gfxViewport(self, x: int, y: int, width: int, height: int): pass

    def test__init__(self): pass
    def test_event(self):
        self.event(GLCamera.EventType.MouseEnter, None, None)
        self.event(GLCamera.EventType.MouseLeave, None, None)
    def test_setViewport(self):
        self.setViewport(0, 0, 100, 100)

# TestGLDebugCamera
class TestGLDebugCamera(GLDebugCamera, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()
        self.mouseOverRenderArea = True
        self.setViewport(0, 0, 100, 100)
    def gfxViewport(self, x: int, y: int, width: int, height: int): pass

    def test__init__(self): pass
    def test_tick(self):
        self.tick(1)
    def test_handleInput(self):
        self.handleInput(MouseState(), KeyboardState())
    def test__handleInputTick(self):
        self._handleInputTick(1.)

#endregion

#region Model

# TestGLMeshBuffers
class TestGLMeshBuffers(GLMeshBuffers, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(None)

    def test__init__(self):
        self.assertEqual(0., self.pitch)
    
# TestGLMeshBufferCache
class TestGLMeshBufferCache(GLMeshBufferCache, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestMeshBatchRenderer
class TestGLMeshBufferCache(MeshBatchRenderer, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestQuadIndexBuffer
class TestQuadIndexBuffer(QuadIndexBuffer, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestGLPickingTexture
class TestGLPickingTexture(GLPickingTexture, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestGLRenderMaterial
class TestGLRenderMaterial(GLRenderMaterial, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestGLRenderableMesh
class TestGLRenderableMesh(GLRenderableMesh, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        self.assertEqual(0., self.pitch)

#endregion

#region Scene

# TestOctreeDebugRenderer
class TestOctreeDebugRenderer(OctreeDebugRenderer, TestCase): 
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(None, None, None)

    def test__init__(self):
        # print(timer)
        #self.assertEqual(timer, 0)
        pass

    # def test_zero(self):
    #     self.assertEqual(abs(0), 0)

#endregion

#region Particle

#endregion

if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=1)
