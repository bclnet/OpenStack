import numpy as np
from unittest import TestCase, main
from gl_render import GLMeshBuffers, GLMeshBufferCache, MeshBatchRenderer, QuadIndexBuffer, GLPickingTexture, GLRenderMaterial, GLRenderableMesh

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

if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=1)