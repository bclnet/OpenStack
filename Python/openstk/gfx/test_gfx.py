from unittest import TestCase, main
from gfx import GfxX

# TestGfxX
class TestGfxX(TestCase):
    def test__init__(self):
        self.assertEqual(0, GfxX.maxTextureMaxAnisotropy)

if __name__ == "__main__":
    main(verbosity=1)