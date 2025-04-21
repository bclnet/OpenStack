import numpy as np
from unittest import TestCase, main
from gl import ShaderLoader, ShaderDebugLoader

# TestShaderLoader
class TestShaderLoader(ShaderLoader, TestCase): 
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        # print(timer)
        #self.assertEqual(timer, 0)
        pass

    # def test_zero(self):
    #     self.assertEqual(abs(0), 0)

# TestShaderDebugLoader
class TestShaderDebugLoader(ShaderDebugLoader, TestCase): 
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__()

    def test__init__(self):
        # print(timer)
        #self.assertEqual(timer, 0)
        pass

    # def test_zero(self):
    #     self.assertEqual(abs(0), 0)


if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=2)