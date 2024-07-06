import unittest
from gfx_bitmap import DirectBitmap

# TestDirectBitmap
class TestDirectBitmap(unittest.TestCase, DirectBitmap):
    def __init__(self, method: str):
        super().__init__(method)
        DirectBitmap.__init__(100, 100)

    def test_setPixel(self):
        self.setPixel(0, 0, 10)
    def test_getPixel(self):
        actual = self.getPixel(0, 0)
        self.assertEqual(0, actual)
    def test_save(self):
        self.save('path')

if __name__ == "__main__":
    unittest.main(verbosity=2)