import os

# DirectBitmap
class DirectBitmap:
    width: int
    height: int
    pixels: bytes
    def __init__(self, width: int, height: int):
        self.width = width
        self.height = height
        self.pixels = bytes(width * height * 4)
    def setPixel(self, x: int, y: int, color: int) -> None: pass
    def getPixel(self, x: int, y: int) -> int: return 0
    def save(self, path: str) -> None: pass