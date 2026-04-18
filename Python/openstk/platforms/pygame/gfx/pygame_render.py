from __future__ import annotations
import os, numpy as np
import pygame
# from pygame.locals import *
from openstk.gfx import Renderer

# typedefs
class PygameGfxModel: pass

#region PygameTestAnimRenderer

# PygameTextureRenderer
class PygameTestAnimRenderer(Renderer):
    def __init__(self, gfx: PygameGfxModel, obj: object, surf: object):
        self.surf = surf

    def start(self) -> None:
        self.x = 320 # Initial x position of the moving object
        self.dx = 5 # Speed of the moving object

    def update(self, deltaTime: float) -> None:
        w = self.surf.get_width()
        # Draw the moving object
        pygame.draw.circle(self.surf, (0, 0, 0), (self.x, 240), 30)  # Draw a black circle at the current
        # position
        self.x += self.dx # Update the position of the moving object
        if self.x + 30 > w or self.x - 30 < 0: # Check if the moving object has reached the edge of the surface
            self.dx = -self.dx # Reverse the direction of the moving object

#endregion

#region PygameTestTriRenderer

# PygameTestTriRenderer
class PygameTestTriRenderer(Renderer):
    def __init__(self, gfx: PygameGfxModel, obj: object, surf: object): pass

#endregion
