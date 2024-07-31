from unittest import TestCase, main
from gfx_bitmap import DirectBitmap

# TestDirectBitmap
class TestDirectBitmap(DirectBitmap, TestCase):
    def __init__(self, method: str):
        TestCase.__init__(self, method)
        super().__init__(100, 100)

    def test__init__(self):
        self.assertEqual(100, self.width)
        self.assertEqual(100, self.height)
    def test_setPixel(self):
        self.setPixel(0, 0, 10)
    def test_getPixel(self):
        actual = self.getPixel(0, 0)
        self.assertEqual(0, actual)
    def test_save(self):
        self.save('path')

if __name__ == "__main__":
    main(verbosity=1)