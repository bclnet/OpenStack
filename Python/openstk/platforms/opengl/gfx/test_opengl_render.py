import numpy as np
from unittest import TestCase, main
from gl_render import TextureRenderer, MaterialRenderer, ParticleGridRenderer

# TestTextureRenderer
class TestTextureRenderer(TextureRenderer, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(None, 0, False)

    def test__init__(self):
        self.assertEqual(0., self.pitch)

# TestMaterialRenderer
class TestMaterialRenderer(MaterialRenderer, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(None, None)

    def test__init__(self):
        self.assertEqual(0., self.pitch)
    
# TestParticleGridRenderer
class TestParticleGridRenderer(ParticleGridRenderer, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(None, 1., 1)

    def test__init__(self):
        self.assertEqual(0., self.pitch)

if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=1)