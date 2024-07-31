from unittest import TestCase, main
from poly_reader import PlatformStats

# TestPlatformStats
class TestPlatformStats(TestCase):
    def test__init__(self):
        self.assertEqual(0, PlatformStats.maxTextureMaxAnisotropy)

if __name__ == "__main__":
    main(verbosity=1)