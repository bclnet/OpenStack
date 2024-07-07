import sys
from unittest import TestCase, main

# @unittest.skipUnless(sys.platform.startswith("xwin"), "Requires Windows")
# class GlView(TestCase):
#     def test_zero(self):
#         self.assertEqual(abs(0), 0)

if __name__ == "__main__":
    import pygame
    from pygame.locals import *
    pygame.init()
    pygame.display.set_mode((200, 200), HWSURFACE|OPENGL|DOUBLEBUF)
    main(verbosity=2)