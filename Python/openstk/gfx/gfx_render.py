from __future__ import annotations
import math, numpy as np
from enum import Enum

#region Color

#endregion

#region Renderer

# Renderer
class Renderer:
    # Pass
    class Pass(Enum):
        Both = 0,
        Opaque = 1,
        Translucent = 2 # Blended
    def start(self) -> None: pass
    def stop(self) -> None: pass
    def update(self, deltaTime: float) -> None: pass
    def dispose(self) -> None: pass

#endregion

#region Raster

# Raster
class Raster:
    @staticmethod
    def blitPalette(data: bytearray, bbp: int, source: bytes, palette: bytes, pbp: int, alpha: int = None) -> None:
        pi = 0
        if pbp == 3:
            if bbp == 4:
                if alpha == None:
                    for i, s in enumerate(source):
                        p = s * 3
                        data[pi + 0] = palette[p + 0]
                        data[pi + 1] = palette[p + 1]
                        data[pi + 2] = palette[p + 2]
                        data[pi + 3] = 0xFF
                        pi += 4
                else:
                    a = alpha
                    for i, s in enumerate(source):
                        p = s * 3
                        data[pi + 0] = palette[p + 0]
                        data[pi + 1] = palette[p + 1]
                        data[pi + 2] = palette[p + 2]
                        data[pi + 3] = 0x00 if s == a else 0xFF
                        pi += 4
            elif bbp == 3:
                for i, s in enumerate(source):
                    p = s * 3
                    data[pi + 0] = palette[p + 0]
                    data[pi + 1] = palette[p + 1]
                    data[pi + 2] = palette[p + 2]
                    pi += 3
        elif pbp == 4:
            if bbp == 4:
                for i, s in enumerate(source):
                    p = s * 4
                    data[pi + 0] = palette[p + 0]
                    data[pi + 1] = palette[p + 1]
                    data[pi + 2] = palette[p + 2]
                    data[pi + 3] = palette[p + 3]
                    pi += 4
            elif bbp == 3:
                for i, s in enumerate(source):
                    p = s * 4
                    data[pi + 0] = palette[p + 0]
                    data[pi + 1] = palette[p + 1]
                    data[pi + 2] = palette[p + 2]
                    pi += 3

#endregion