from unittest import TestCase, main
from gfx import GfX

# TestGfX
class TestGfX(TestCase):
    def test__init__(self):
        self.assertEqual(0, GfX.maxTextureMaxAnisotropy)

if __name__ == "__main__":
    main(verbosity=1)